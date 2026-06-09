using System.IO;
using BingChengAssistant.Data;
using BingChengAssistant.Models;
using Microsoft.Data.Sqlite;

namespace BingChengAssistant.Services;

/// <summary>
/// 将旧版 WinForms（v1.x）数据库迁移到新版 WPF（v1.2）数据库
/// 旧库路径：%LocalAppData%\MedicalProgress\medical_progress.db
/// 旧库表：Patients / ProgressRecords（EF Core 自动命名）
/// </summary>
public static class LegacyMigrationService
{
    public static string LegacyDbPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MedicalProgress", "medical_progress.db");

    public static bool LegacyDbExists() => File.Exists(LegacyDbPath);

    /// <summary>
    /// 执行迁移，返回导入统计
    /// </summary>
    public static (int patients, int notes, string error) Migrate(int targetDoctorId)
    {
        if (!LegacyDbExists())
            return (0, 0, $"找不到旧版数据库：{LegacyDbPath}");

        try
        {
            // 读取旧库所有患者
            var oldPatients = ReadLegacyPatients();
            var oldNotes = ReadLegacyNotes();

            if (oldPatients.Count == 0)
                return (0, 0, "旧版数据库中没有患者数据");

            var repo = new PatientRepository();
            int pCount = 0, nCount = 0;

            foreach (var op in oldPatients)
            {
                // 检查是否已存在（按姓名+入院日期去重）
                if (AdmissionExists(op.Name, op.AdmissionDate))
                    continue;

                var patient = new Patient
                {
                    Name = op.Name,
                    Gender = op.Gender,
                    Age = op.Age,
                    PastHistory = op.History,
                };
                int pid = repo.InsertPatient(patient);

                var adm = new Admission
                {
                    PatientId = pid,
                    DoctorId = targetDoctorId,
                    AdmissionNo = op.MedicalRecordNumber,
                    BedNo = op.BedNumber,
                    Department = op.Department,
                    AdmissionDate = op.AdmissionDate,
                    DischargeDate = op.DischargeDate,
                    MainDiagnosis = op.Diagnosis,
                    Status = op.IsDischarged ? "已出院" : "在院",
                    DischargeOrders = op.DischargeOrders,
                };
                int aid = repo.InsertAdmission(adm);
                repo.BindDoctorPatient(targetDoctorId, aid);
                pCount++;

                // 迁移该患者的病程记录
                var noteRepo = new ProgressNoteRepository();
                foreach (var on in oldNotes.Where(n => n.PatientId == op.Id))
                {
                    noteRepo.Insert(new ProgressNote
                    {
                        AdmissionId = aid,
                        DoctorId = targetDoctorId,
                        NoteType = on.RecordType,
                        Content = on.Content,
                        RecordDate = on.RecordDate,
                    });
                    nCount++;
                }
            }

            OperationLogService.Log("迁移旧版数据", $"患者:{pCount} 病程:{nCount}");
            return (pCount, nCount, "");
        }
        catch (Exception ex)
        {
            LogService.Error("迁移旧版数据失败", ex);
            return (0, 0, ex.Message);
        }
    }

    private static bool AdmissionExists(string name, DateTime admDate)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
SELECT COUNT(*) FROM admissions a JOIN patients p ON p.id=a.patient_id
WHERE p.name=@name AND a.admission_date=@date
""";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@date", admDate.ToString("yyyy-MM-dd"));
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    private static List<LegacyPatient> ReadLegacyPatients()
    {
        var list = new List<LegacyPatient>();
        var cs = $"Data Source={LegacyDbPath};Mode=ReadOnly";
        using var conn = new SqliteConnection(cs);
        conn.Open();
        using var cmd = conn.CreateCommand();
        // EF Core 默认表名 Patients，列名与属性名一致
        cmd.CommandText = """
SELECT Id, Name, Gender, Age, BedNumber, MedicalRecordNumber, Department,
       AttendingDoctor, Diagnosis, ChiefComplaint, History,
       AdmissionDate, DischargeDate, IsDischarged, DischargeOrders
FROM Patients ORDER BY AdmissionDate
""";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new LegacyPatient
            {
                Id = r.GetInt32(0),
                Name = r["Name"]?.ToString() ?? "",
                Gender = r["Gender"]?.ToString() ?? "男",
                Age = Convert.ToInt32(r["Age"]),
                BedNumber = r["BedNumber"]?.ToString() ?? "",
                MedicalRecordNumber = r["MedicalRecordNumber"]?.ToString() ?? "",
                Department = r["Department"]?.ToString() ?? "",
                AttendingDoctor = r["AttendingDoctor"]?.ToString() ?? "",
                Diagnosis = r["Diagnosis"]?.ToString() ?? "",
                ChiefComplaint = r["ChiefComplaint"]?.ToString() ?? "",
                History = r["History"]?.ToString() ?? "",
                AdmissionDate = r["AdmissionDate"] == DBNull.Value
                    ? DateTime.Today
                    : DateTime.Parse(r["AdmissionDate"].ToString()!),
                DischargeDate = r["DischargeDate"] == DBNull.Value
                    ? null
                    : DateTime.Parse(r["DischargeDate"].ToString()!),
                IsDischarged = Convert.ToInt32(r["IsDischarged"]) == 1,
                DischargeOrders = r["DischargeOrders"]?.ToString() ?? "",
            });
        }
        return list;
    }

    private static List<LegacyNote> ReadLegacyNotes()
    {
        var list = new List<LegacyNote>();
        var cs = $"Data Source={LegacyDbPath};Mode=ReadOnly";
        using var conn = new SqliteConnection(cs);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, PatientId, RecordDate, Content, RecordType FROM ProgressRecords ORDER BY RecordDate";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new LegacyNote
            {
                Id = r.GetInt32(0),
                PatientId = Convert.ToInt32(r["PatientId"]),
                RecordDate = r["RecordDate"] == DBNull.Value
                    ? DateTime.Now
                    : DateTime.Parse(r["RecordDate"].ToString()!),
                Content = r["Content"]?.ToString() ?? "",
                RecordType = r["RecordType"]?.ToString() ?? "日常病程",
            });
        }
        return list;
    }

    private class LegacyPatient
    {
        public int Id;
        public string Name = "", Gender = "男", BedNumber = "", MedicalRecordNumber = "",
            Department = "", AttendingDoctor = "", Diagnosis = "",
            ChiefComplaint = "", History = "", DischargeOrders = "";
        public int Age;
        public DateTime AdmissionDate = DateTime.Today;
        public DateTime? DischargeDate;
        public bool IsDischarged;
    }

    private class LegacyNote
    {
        public int Id, PatientId;
        public DateTime RecordDate;
        public string Content = "", RecordType = "日常病程";
    }
}
