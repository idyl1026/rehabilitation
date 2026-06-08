"""
病程资料 → 标准病程模板 转换脚本
====================================
用途：批量读取 .txt / .docx 资料文件，通过 DeepSeek API 转换为
      标准病程模板格式，输出 CSV 文件供程序导入。

使用方法：
1. 安装依赖：pip install openai python-docx
2. 设置 API Key（见下方 API_KEY）
3. 修改 INPUT_FOLDER 和 OUTPUT_CSV 路径
4. 运行：python convert_to_templates.py

输出 CSV 可直接在程序中【知识库管理 → 📊批量导入】使用。
"""

import os
import csv
import json
import time
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path
from openai import OpenAI

# ============================================================
# ★ 配置区 - 按需修改
# ============================================================

# DeepSeek API Key（在 https://platform.deepseek.com 获取）
API_KEY = "sk-3ecd63c47ac847a2ada9c34ab5416030"

# 要处理的资料文件夹列表（支持子目录）
INPUT_FOLDERS = [
    r"D:\vibecoding\病程模板\杨资料勿删",
    r"D:\vibecoding\病程模板\杨资料误删",
    r"D:\vibecoding\病程模板\章旭专  勿删",
]

# 输出的 CSV 文件路径
OUTPUT_CSV = r"D:\vibecoding\病程模板\converted_templates.csv"

# 是否跳过已处理文件（断点续传）
RESUME_MODE = True

# 每次 API 调用间隔（秒），避免限流
API_DELAY = 1.0

# 单文件内容超过此长度时截断再发给AI（节省token）
MAX_CONTENT_LENGTH = 3000

# ============================================================
# DeepSeek 客户端初始化
# ============================================================

client = OpenAI(
    api_key=API_KEY,
    base_url="https://api.deepseek.com"
)

# ============================================================
# 文件读取工具
# ============================================================

def read_txt(filepath: str) -> str:
    """读取 .txt 文件，自动处理编码"""
    for encoding in ["utf-8", "gbk", "utf-8-sig", "gb2312"]:
        try:
            with open(filepath, "r", encoding=encoding) as f:
                return f.read().strip()
        except (UnicodeDecodeError, LookupError):
            continue
    return ""


def read_docx(filepath: str) -> str:
    """读取 .docx 文件，解析 XML 提取纯文本"""
    try:
        ns = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
        with zipfile.ZipFile(filepath, "r") as z:
            with z.open("word/document.xml") as f:
                tree = ET.parse(f)
                root = tree.getroot()
                paragraphs = []
                for p in root.iter(f"{{{ns}}}p"):
                    texts = [t.text or "" for t in p.iter(f"{{{ns}}}t")]
                    line = "".join(texts).strip()
                    if line:
                        paragraphs.append(line)
                return "\n".join(paragraphs)
    except Exception as e:
        print(f"  ⚠️  读取 docx 失败：{e}")
        return ""


def read_file(filepath: str) -> str:
    """根据扩展名分发读取"""
    ext = Path(filepath).suffix.lower()
    if ext == ".txt":
        return read_txt(filepath)
    elif ext == ".docx":
        return read_docx(filepath)
    return ""


def collect_files(folders: list[str]) -> list[str]:
    """递归收集多个文件夹下的 .txt 和 .docx 文件"""
    result = []
    for folder in folders:
        for root, _, files in os.walk(folder):
            for f in files:
                if f.lower().endswith((".txt", ".docx")) and not f.startswith("~$"):
                    result.append(os.path.join(root, f))
    return sorted(result)


# ============================================================
# 分类和类型猜测（快速启发式，不用 API）
# ============================================================

CATEGORY_HINTS = {
    "神经系统": ["脑", "神经", "脊髓", "瘫", "卒中", "梗死", "出血", "面瘫", "截瘫"],
    "骨关节":   ["骨", "关节", "椎", "膝", "肩", "颈", "腰", "股骨", "韧带", "半月板"],
    "鉴别诊断": ["鉴别", "诊断"],
    "治疗方案": ["治疗", "药物", "用药", "康复", "手术", "物理因子"],
    "病程记录": ["病程", "记录"],
}

TYPE_HINTS = {
    "首次病程": ["首次", "入院记录", "病例特点", "诊疗计划"],
    "出院小结": ["出院", "出院小结", "住院经过"],
    "会诊记录": ["会诊", "邀请会诊"],
    "鉴别诊断": ["鉴别诊断", "鉴别"],
    "治疗方案": ["治疗方案", "药物", "康复方案"],
}


def guess_category(filepath: str, content: str) -> str:
    combined = filepath + content[:200]
    for cat, keywords in CATEGORY_HINTS.items():
        if any(k in combined for k in keywords):
            return cat
    return "其他"


def guess_type(filepath: str, content: str) -> str:
    combined = Path(filepath).stem + content[:300]
    for t, keywords in TYPE_HINTS.items():
        if any(k in combined for k in keywords):
            return t
    return "日常病程"


# ============================================================
# DeepSeek API 转换
# ============================================================

SYSTEM_PROMPT = """你是一位经验丰富的住院医师，擅长书写规范的康复医学科病程记录。
你的任务是将输入的医学资料文字改写为标准病程模板。

输出要求（JSON格式）：
{
  "title": "模板名称（简洁，如：脑梗死恢复期日常病程）",
  "type": "模板类型（从以下选一：首次病程、日常病程、出院小结、会诊记录、鉴别诊断、治疗方案）",
  "category": "疾病分类（从以下选一：神经系统、骨关节、鉴别诊断、治疗方案、病程记录、其他）",
  "keywords": "关键词，英文逗号分隔（3-6个，如：脑梗死,偏瘫,康复）",
  "content": "标准病程模板正文（保留医学规范格式，用{...}标记需医生填写的占位内容，如{主诉}、{体格检查结果}、{VAS评分}）"
}

病程模板正文规范：
- 首次病程：包含病例特点、诊断依据、鉴别诊断、诊疗计划四部分
- 日常病程：包含病情变化、治疗情况、查体情况、下一步计划四部分
- 出院小结：包含入院情况、诊疗经过、出院情况、出院医嘱四部分
- 用"{内容描述}"格式标注需要医生根据患者实际情况填写的位置
- 保留专业医学术语，不要过度简化
- 只返回 JSON，不要任何说明文字"""


def convert_with_ai(filename: str, content: str, category_hint: str, type_hint: str) -> dict | None:
    """调用 DeepSeek API 将原始资料转换为结构化模板"""

    # 截断过长内容
    if len(content) > MAX_CONTENT_LENGTH:
        content = content[:MAX_CONTENT_LENGTH] + "\n...(内容已截断)"

    user_prompt = f"""请将以下医学资料转换为标准病程模板。

文件名：{filename}
预判分类：{category_hint}
预判类型：{type_hint}

原始内容：
{content}

请按要求输出 JSON。"""

    try:
        response = client.chat.completions.create(
            model="deepseek-chat",
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": user_prompt},
            ],
            temperature=0.3,
            max_tokens=2000,
            response_format={"type": "json_object"},
        )

        raw = response.choices[0].message.content
        data = json.loads(raw)

        # 验证必填字段
        if not data.get("title") or not data.get("content"):
            print("  ⚠️  API 返回字段不完整，跳过")
            return None

        return data

    except json.JSONDecodeError as e:
        print(f"  ⚠️  JSON 解析失败：{e}")
        return None
    except Exception as e:
        print(f"  ⚠️  API 调用失败：{e}")
        return None


# ============================================================
# 断点续传：记录已处理文件
# ============================================================

PROGRESS_FILE = OUTPUT_CSV + ".progress.json"


def load_processed() -> set:
    if RESUME_MODE and os.path.exists(PROGRESS_FILE):
        with open(PROGRESS_FILE, "r", encoding="utf-8") as f:
            return set(json.load(f))
    return set()


def save_processed(processed: set):
    with open(PROGRESS_FILE, "w", encoding="utf-8") as f:
        json.dump(list(processed), f, ensure_ascii=False)


# ============================================================
# 主流程
# ============================================================

def main():
    print("=" * 60)
    print("病程资料 → 标准病程模板 转换工具")
    print("=" * 60)
    print(f"输入目录：")
    for f in INPUT_FOLDERS:
        print(f"  · {f}")
    print(f"输出文件：{OUTPUT_CSV}")
    print()

    # 收集所有文件夹文件
    all_files = collect_files(INPUT_FOLDERS)
    if not all_files:
        print("❌ 未找到任何 .txt 或 .docx 文件，请检查 INPUT_FOLDERS 路径")
        return

    print(f"共找到 {len(all_files)} 个文件：")
    for f in INPUT_FOLDERS:
        cnt = len([x for x in all_files if x.startswith(f)])
        print(f"  · {os.path.basename(f)}：{cnt} 个")
    print()

    # 加载已处理记录（断点续传）
    processed = load_processed()
    if processed:
        print(f"🔄 已处理 {len(processed)} 个文件（断点续传模式）")

    # 准备 CSV 输出
    file_exists = os.path.exists(OUTPUT_CSV)
    csv_mode = "a" if (RESUME_MODE and file_exists) else "w"

    success_count = 0
    skip_count = 0
    fail_count = 0

    with open(OUTPUT_CSV, csv_mode, newline="", encoding="utf-8-sig") as csvfile:
        writer = csv.writer(csvfile)

        # 首次写入才写表头
        if csv_mode == "w" or not file_exists:
            writer.writerow(["标题", "类型", "分类", "关键词", "内容"])

        for i, filepath in enumerate(all_files):
            filename = Path(filepath).name
            # 找出属于哪个父文件夹，显示相对路径
            for folder in INPUT_FOLDERS:
                if filepath.startswith(folder):
                    rel_path = os.path.relpath(filepath, folder)
                    break

            # 跳过已处理
            if filepath in processed:
                skip_count += 1
                continue

            print(f"[{i+1}/{len(all_files)}] 处理：{rel_path}")

            # 读取内容
            content = read_file(filepath)
            if not content or len(content) < 50:
                print("  ⚠️  内容过短，跳过")
                fail_count += 1
                processed.add(filepath)
                save_processed(processed)
                continue

            # 启发式猜测分类和类型
            cat_hint = guess_category(filepath, content)
            type_hint = guess_type(filepath, content)

            # 调用 AI 转换
            result = convert_with_ai(filename, content, cat_hint, type_hint)

            if result:
                writer.writerow([
                    result.get("title", filename),
                    result.get("type", type_hint),
                    result.get("category", cat_hint),
                    result.get("keywords", ""),
                    result.get("content", content),
                ])
                csvfile.flush()  # 实时写入磁盘
                success_count += 1
                print(f"  ✅  {result['title']} [{result['type']}]")
            else:
                # AI 失败时，保存原始内容（不丢数据）
                writer.writerow([
                    Path(filepath).stem,
                    type_hint,
                    cat_hint,
                    "",
                    content[:MAX_CONTENT_LENGTH],
                ])
                csvfile.flush()
                fail_count += 1
                print("  ⚠️  AI 转换失败，已保存原始内容")

            processed.add(filepath)
            save_processed(processed)

            # 限流延迟
            time.sleep(API_DELAY)

    print()
    print("=" * 60)
    print(f"✅ 完成！")
    print(f"   成功转换：{success_count} 个")
    print(f"   AI失败保存原文：{fail_count} 个")
    print(f"   已跳过（断点续传）：{skip_count} 个")
    print(f"   输出文件：{OUTPUT_CSV}")
    print()
    print("下一步：在程序中 知识库管理 → 📊批量导入 → 选择上面的CSV文件")
    print("=" * 60)


if __name__ == "__main__":
    main()
