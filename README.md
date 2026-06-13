# 病程助手（BingChengAssistant）

康复医学科**病程辅助书写工具** · C# .NET 8 WPF 桌面应用 · 内网单机版 · 无需联网，不替代正式电子病历系统。

> 仅用于辅助医生书写病程、康复评估与出院归档，所有数据保存在本机。

---

## 功能

- **医生登录/注册**，多医生独立工作区
- **患者管理**：建档、编辑、删除，自动为每位患者创建 Word 病例文档
- **智能病程录入**
  - 结构化录入：主诉 / 现病史 / 辅助检查 / 康复评估分字段
  - 新建病程自动带入上次主诉、现病史（无既往则取首次病程模板）
  - 「一键整理」规则化组装为标准病程，并按诊断自动匹配知识卡片
  - 匹配卡片可更换 / 移除；重复插入弹窗提醒、强制插入标红
- **知识库**：1200+ 张康复知识卡片（每张 ≤200 字），按病种 / 物理因子 / 药物 / 操作 / 会诊 等分类；支持搜索、分类标签、引用历史前置；可 Excel 批量导入、清空
- **康复评估**：60+ 评定量表（VAS/NRS/MMT/ROM/MBI/BBS/FMA/MoCA/FIM 等），可对照量表全文打分，结果一键带回病程
- **出院归档**：汇总生成 Word 出院记录 + 更新科研索引 Excel
- **设置**：数据备份 / 还原、量表导入、数据库路径查看

---

## 下载与使用

在 [Releases](../../releases/latest) 下载最新版：

1. 下载 `MedNote-Assistant-v*.exe`，双击运行（Windows 10/11 64 位，无需安装）
2. 首次打开登记医生信息并登录
3. 「知识库」→「导入Excel」选择 `knowledge_import.xlsx` 导入知识卡片
4. 「设置」→「导入量表Excel」选择 `scales_import.xlsx` 导入评定量表
5. 软件标题栏显示当前版本号，确认与 Releases 页一致即为最新版

> 国内下载较慢可在链接前加镜像前缀，例如 `https://ghfast.top/`。

---

## 构建

应用通过 GitHub Actions 在 `windows-latest` 上自动构建：推送 `v*` 标签即触发，产出单文件自包含 exe 并发布到 Releases。

本地构建（需 Windows + .NET 8 SDK）：

```cmd
dotnet publish src/BingChengAssistant/BingChengAssistant.csproj ^
  -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -o out
```

---

## 技术栈

- .NET 8 WPF（MVVM）
- SQLite（Microsoft.Data.Sqlite）本地数据库
- DocumentFormat.OpenXml 生成 Word
- ClosedXML 读写 Excel
