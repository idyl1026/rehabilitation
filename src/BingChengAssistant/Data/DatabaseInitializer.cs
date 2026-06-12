using Microsoft.Data.Sqlite;

namespace BingChengAssistant.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var conn = DbConnectionFactory.Create();
        CreateTables(conn);
        SeedDefaults(conn);
    }

    private static void CreateTables(SqliteConnection c)
    {
        var sql = """
CREATE TABLE IF NOT EXISTS doctors (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    employee_no TEXT NOT NULL UNIQUE,
    department TEXT,
    title TEXT,
    pin_hash TEXT DEFAULT '',
    is_default INTEGER DEFAULT 0,
    is_active INTEGER DEFAULT 1,
    created_at TEXT DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS doctor_login_logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    doctor_id INTEGER,
    login_time TEXT DEFAULT (datetime('now','localtime')),
    remark TEXT
);

CREATE TABLE IF NOT EXISTS patients (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    gender TEXT DEFAULT '男',
    age INTEGER DEFAULT 0,
    phone TEXT DEFAULT '',
    allergy_history TEXT DEFAULT '',
    past_history TEXT DEFAULT '',
    remark TEXT DEFAULT '',
    created_at TEXT DEFAULT (datetime('now','localtime')),
    is_active INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS admissions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    patient_id INTEGER NOT NULL,
    doctor_id INTEGER NOT NULL,
    admission_no TEXT DEFAULT '',
    bed_no TEXT DEFAULT '',
    department TEXT DEFAULT '',
    admission_date TEXT NOT NULL,
    discharge_date TEXT,
    main_diagnosis TEXT DEFAULT '',
    secondary_diagnosis TEXT DEFAULT '',
    status TEXT DEFAULT '在院',
    discharge_outcome TEXT DEFAULT '',
    discharge_orders TEXT DEFAULT '',
    rehab_advice TEXT DEFAULT '',
    exercise_prescription TEXT DEFAULT '',
    follow_up_advice TEXT DEFAULT '',
    research_note TEXT DEFAULT '',
    created_at TEXT DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS doctor_patient_map (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    doctor_id INTEGER NOT NULL,
    admission_id INTEGER NOT NULL,
    created_at TEXT DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS patient_insurance_info (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    admission_id INTEGER NOT NULL,
    insurance_type TEXT DEFAULT '',
    insurance_region TEXT DEFAULT ''
);

CREATE TABLE IF NOT EXISTS progress_notes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    admission_id INTEGER NOT NULL,
    doctor_id INTEGER NOT NULL,
    note_type TEXT DEFAULT '日常病程',
    content TEXT DEFAULT '',
    record_date TEXT DEFAULT (datetime('now','localtime')),
    is_synced_to_word INTEGER DEFAULT 0,
    created_at TEXT DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS note_templates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    note_type TEXT DEFAULT '',
    content TEXT DEFAULT '',
    is_built_in INTEGER DEFAULT 0,
    is_active INTEGER DEFAULT 1,
    created_at TEXT DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS rehab_scale_dict (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    description TEXT DEFAULT '',
    scale_type TEXT DEFAULT 'numeric',
    is_active INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS rehab_scale_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    scale_id INTEGER NOT NULL,
    item_name TEXT NOT NULL,
    item_order INTEGER DEFAULT 0,
    score_range TEXT DEFAULT '',
    description TEXT DEFAULT ''
);

CREATE TABLE IF NOT EXISTS rehab_assessment_records (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    admission_id INTEGER NOT NULL,
    doctor_id INTEGER NOT NULL,
    scale_id INTEGER NOT NULL,
    scale_name TEXT DEFAULT '',
    assessment_date TEXT DEFAULT (datetime('now','localtime')),
    result_summary TEXT DEFAULT '',
    interpretation TEXT DEFAULT '',
    rehab_advice TEXT DEFAULT '',
    note_text TEXT DEFAULT '',
    is_synced_to_word INTEGER DEFAULT 0,
    created_at TEXT DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS rehab_assessment_results (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    record_id INTEGER NOT NULL,
    item_name TEXT DEFAULT '',
    score_value TEXT DEFAULT '',
    remark TEXT DEFAULT ''
);

CREATE TABLE IF NOT EXISTS patient_word_docs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    admission_id INTEGER NOT NULL,
    file_path TEXT DEFAULT '',
    file_name TEXT DEFAULT '',
    status TEXT DEFAULT '已创建',
    created_at TEXT DEFAULT (datetime('now','localtime')),
    last_synced_at TEXT
);

CREATE TABLE IF NOT EXISTS research_case_index (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    admission_id INTEGER NOT NULL,
    doctor_id INTEGER NOT NULL,
    patient_name TEXT DEFAULT '',
    admission_no TEXT DEFAULT '',
    main_diagnosis TEXT DEFAULT '',
    admission_date TEXT DEFAULT '',
    discharge_date TEXT,
    doctor_name TEXT DEFAULT '',
    note_count INTEGER DEFAULT 0,
    rehab_count INTEGER DEFAULT 0,
    word_file_path TEXT DEFAULT '',
    research_note TEXT DEFAULT '',
    updated_at TEXT DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS operation_logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    doctor_id INTEGER,
    operation TEXT DEFAULT '',
    detail TEXT DEFAULT '',
    created_at TEXT DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS system_settings (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    key TEXT NOT NULL UNIQUE,
    value TEXT DEFAULT '',
    remark TEXT DEFAULT ''
);

CREATE TABLE IF NOT EXISTS knowledge_base (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    content TEXT DEFAULT '',
    category TEXT DEFAULT '通用',
    tags TEXT DEFAULT '',
    is_active INTEGER DEFAULT 1,
    created_at TEXT DEFAULT (datetime('now','localtime'))
);
""";
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();

        // 升级迁移：旧库 rehab_scale_dict 补 content 列（存完整量表内容）
        try
        {
            using var alter = c.CreateCommand();
            alter.CommandText = "ALTER TABLE rehab_scale_dict ADD COLUMN content TEXT DEFAULT ''";
            alter.ExecuteNonQuery();
        }
        catch { /* 列已存在 */ }
    }

    private static void SeedDefaults(SqliteConnection c)
    {
        // system_settings
        ExecIfEmpty(c, "system_settings",
            "INSERT INTO system_settings (key, value, remark) VALUES " +
            "('app_version','1.2.0','软件版本')," +
            "('auto_login','0','自动登录')," +
            "('default_doctor_id','0','默认医生')");

        // note_templates
        ExecIfEmpty(c, "note_templates", """
INSERT INTO note_templates (name, note_type, content, is_built_in) VALUES
('首次病程记录', '首次病程',
'【首次病程记录】

患者 {姓名}，{性别}，{年龄}岁，因"{主要诊断}"于 {入院日期} 入院。

【主诉】

【现病史】

【既往史】{既往史}

【体格检查】
体温：  脉搏：  呼吸：  血压：
一般情况：
专科检查：

【辅助检查】

【诊断】
1. {主要诊断}

【诊疗计划】
1. 完善相关检查。
2. {处理计划}
3. 向患者及家属交代病情，签署知情同意书。

医师签名：{医生姓名}
记录时间：{今日日期}', 1),

('日常病程记录', '日常病程',
'【日常病程记录】
记录日期：{今日日期}

患者 {姓名}，{性别}，{年龄}岁，入院第  天。诊断：{主要诊断}。

【病情变化】

【体征】
体温：  脉搏：  呼吸：  血压：
一般情况：

【辅助检查结果】

【分析与处理】

【下一步计划】

医师签名：{医生姓名}', 1),

('上级医师查房记录', '上级查房',
'【上级医师查房记录】
查房日期：{今日日期}

患者 {姓名}，{性别}，{年龄}岁，住院号 {住院号}，床号 {床号}。

上级医师查房意见：

1. 病情分析：
2. 诊断意见：
3. 治疗调整意见：
4. 康复治疗意见：

记录医师：{医生姓名}', 1),

('康复评估记录', '康复评估',
'【康复评估记录】
评估日期：{今日日期}

患者 {姓名}，诊断：{主要诊断}。

{康复评估}

康复治疗目标：
短期目标（2周内）：
长期目标（出院时）：

医师签名：{医生姓名}', 1)
""");

        // rehab_scale_dict — 用 INSERT OR IGNORE，老数据库也能补全新量表
        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = """
INSERT OR IGNORE INTO rehab_scale_dict (code, name, description, scale_type) VALUES
('VAS', 'VAS 疼痛评分', '视觉模拟评分法，0-10分，0无痛，10最剧烈疼痛', 'numeric'),
('NRS', 'NRS 疼痛评分', '数字评分法，0-10分，与VAS相同分级', 'numeric'),
('ROM', 'ROM 关节活动度', '测量关节主动和被动活动范围', 'composite'),
('MMT', 'MMT 肌力评估', '徒手肌力测试，0-5级分级', 'grade'),
('MBI', '改良Barthel指数(MBI)', '日常生活活动能力评定，0-100分，100分完全独立', 'generic'),
('BBS', 'Berg平衡量表(BBS)', '平衡功能评定，0-56分，<40分有跌倒风险', 'generic'),
('FMA', 'Fugl-Meyer运动功能(FMA)', '脑卒中运动功能评定，上肢66分+下肢34分=100分', 'generic'),
('MAS', '改良Ashworth痉挛分级(MAS)', '肌张力评定，0-4级，0级无肌张力增高', 'generic'),
('HOLDEN', 'Holden步行功能分级', '步行能力评定，0-5级，5级完全独立步行', 'generic'),
('MOCA', 'MoCA认知评估', '认知功能筛查，0-30分，≥26分正常', 'generic'),
('SAS', 'SAS焦虑自评量表', '焦虑状态评定，标准分≥50分提示焦虑', 'generic'),
('FIM', 'FIM功能独立性评定', '综合功能评定，18-126分，126分完全独立', 'generic')
""";
            cmd.ExecuteNonQuery();
        }

        // knowledge_base 默认知识条目
        ExecIfEmpty(c, "knowledge_base", """
INSERT INTO knowledge_base (title, content, category, tags) VALUES
('腰椎间盘突出症康复要点',
'急性期（1-2周）：卧床休息不超过3天，避免久坐和弯腰负重；可行腰椎牵引、超短波等物理治疗缓解疼痛。
缓解期：核心肌群训练（桥式运动、平板支撑渐进），麦肯基疗法伸展训练。
恢复期：有氧运动（游泳、快走），纠正不良姿势，腰围保护下逐步恢复日常活动。',
'骨科', '腰椎间盘突出,腰痛,核心训练'),

('脑卒中偏瘫早期康复原则',
'生命体征稳定48小时后即可开始床旁康复。
良肢位摆放：患侧卧位为主，避免患侧上肢受压。
早期活动：被动关节活动度训练每日2次，每个关节10-20次，防止关节挛缩。
坐位平衡训练→站立训练→步行训练循序渐进。
注意：避免过度用力牵拉患侧肩关节，预防肩痛和肩关节半脱位。',
'神经', '脑卒中,偏瘫,良肢位,早期康复'),

('膝关节置换术后康复计划',
'术后第1天：踝泵运动、股四头肌等长收缩，CPM机辅助屈膝30-40°。
术后1周内：主动屈膝达90°，直腿抬高训练，助行器辅助部分负重行走。
术后2-6周：逐步增加屈膝角度至120°，渐进负重至完全负重。
术后6-12周：上下楼梯训练、平衡训练、本体感觉训练。',
'骨科', '膝关节置换,TKA,术后康复'),

('颈椎病运动疗法',
'颈部等长抗阻训练：双手抵额头/枕部对抗，每次保持5-10秒，10次/组，每日3组。
肩胛带稳定训练：耸肩、扩胸、靠墙天使动作。
避免：长时间低头、颈部快速旋转、高枕睡眠。
牵引适应证：神经根型颈椎病；禁忌：脊髓型颈椎病慎用牵引。',
'骨科', '颈椎病,运动疗法,等长训练'),

('运动处方基本原则(FITT)',
'F-频率(Frequency)：有氧运动每周3-5次，抗阻训练每周2-3次。
I-强度(Intensity)：中等强度为最大心率的64-76%（最大心率=220-年龄）。
T-时间(Time)：每次30-60分钟，可分段累计。
T-类型(Type)：根据个体情况选择有氧、抗阻、柔韧性、平衡训练。
慢性病患者从低强度开始，循序渐进，运动中监测心率和自觉疲劳程度(RPE 11-13)。',
'通用', '运动处方,FITT,有氧运动')
""");
    }

    private static void ExecIfEmpty(SqliteConnection c, string table, string insertSql)
    {
        using var check = c.CreateCommand();
        check.CommandText = $"SELECT COUNT(*) FROM {table}";
        var count = Convert.ToInt64(check.ExecuteScalar());
        if (count == 0)
        {
            using var ins = c.CreateCommand();
            ins.CommandText = insertSql;
            ins.ExecuteNonQuery();
        }
    }
}
