using BingChengAssistant.Models;
using Microsoft.Data.Sqlite;

namespace BingChengAssistant.Data;

public class PatientRepository
{
    public int InsertPatient(Patient p)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
INSERT INTO patients (name, gender, age, phone, allergy_history, past_history, remark)
VALUES (@name, @gender, @age, @phone, @allergy, @past, @remark);
SELECT last_insert_rowid();
""";
        cmd.Parameters.AddWithValue("@name", p.Name);
        cmd.Parameters.AddWithValue("@gender", p.Gender);
        cmd.Parameters.AddWithValue("@age", p.Age);
        cmd.Parameters.AddWithValue("@phone", p.Phone);
        cmd.Parameters.AddWithValue("@allergy", p.AllergyHistory);
        cmd.Parameters.AddWithValue("@past", p.PastHistory);
        cmd.Parameters.AddWithValue("@remark", p.Remark);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void UpdatePatient(Patient p)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
UPDATE patients SET name=@name, gender=@gender, age=@age, phone=@phone,
allergy_history=@allergy, past_history=@past, remark=@remark WHERE id=@id
""";
        cmd.Parameters.AddWithValue("@name", p.Name);
        cmd.Parameters.AddWithValue("@gender", p.Gender);
        cmd.Parameters.AddWithValue("@age", p.Age);
        cmd.Parameters.AddWithValue("@phone", p.Phone);
        cmd.Parameters.AddWithValue("@allergy", p.AllergyHistory);
        cmd.Parameters.AddWithValue("@past", p.PastHistory);
        cmd.Parameters.AddWithValue("@remark", p.Remark);
        cmd.Parameters.AddWithValue("@id", p.Id);
        cmd.ExecuteNonQuery();
    }

    public Patient? GetById(int id)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM patients WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? MapPatient(r) : null;
    }

    public int InsertAdmission(Admission a)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
INSERT INTO admissions (patient_id, doctor_id, admission_no, bed_no, department,
  admission_date, main_diagnosis, secondary_diagnosis, status)
VALUES (@pid, @did, @ano, @bed, @dept, @admdate, @diag, @diag2, @status);
SELECT last_insert_rowid();
""";
        cmd.Parameters.AddWithValue("@pid", a.PatientId);
        cmd.Parameters.AddWithValue("@did", a.DoctorId);
        cmd.Parameters.AddWithValue("@ano", a.AdmissionNo);
        cmd.Parameters.AddWithValue("@bed", a.BedNo);
        cmd.Parameters.AddWithValue("@dept", a.Department);
        cmd.Parameters.AddWithValue("@admdate", a.AdmissionDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@diag", a.MainDiagnosis);
        cmd.Parameters.AddWithValue("@diag2", a.SecondaryDiagnosis);
        cmd.Parameters.AddWithValue("@status", a.Status);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void UpdateAdmission(Admission a)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
UPDATE admissions SET admission_no=@ano, bed_no=@bed, department=@dept,
  admission_date=@admdate, discharge_date=@disdate, main_diagnosis=@diag,
  secondary_diagnosis=@diag2, status=@status, discharge_outcome=@outcome,
  discharge_orders=@orders, rehab_advice=@rehab, exercise_prescription=@ex,
  follow_up_advice=@follow, research_note=@research
WHERE id=@id
""";
        cmd.Parameters.AddWithValue("@ano", a.AdmissionNo);
        cmd.Parameters.AddWithValue("@bed", a.BedNo);
        cmd.Parameters.AddWithValue("@dept", a.Department);
        cmd.Parameters.AddWithValue("@admdate", a.AdmissionDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@disdate", a.DischargeDate.HasValue ? a.DischargeDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);
        cmd.Parameters.AddWithValue("@diag", a.MainDiagnosis);
        cmd.Parameters.AddWithValue("@diag2", a.SecondaryDiagnosis);
        cmd.Parameters.AddWithValue("@status", a.Status);
        cmd.Parameters.AddWithValue("@outcome", a.DischargeOutcome);
        cmd.Parameters.AddWithValue("@orders", a.DischargeOrders);
        cmd.Parameters.AddWithValue("@rehab", a.RehabAdvice);
        cmd.Parameters.AddWithValue("@ex", a.ExercisePrescription);
        cmd.Parameters.AddWithValue("@follow", a.FollowUpAdvice);
        cmd.Parameters.AddWithValue("@research", a.ResearchNote);
        cmd.Parameters.AddWithValue("@id", a.Id);
        cmd.ExecuteNonQuery();
    }

    public List<Admission> GetAdmissionsByDoctor(int doctorId, string statusFilter = "")
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        var where = string.IsNullOrEmpty(statusFilter)
            ? "a.doctor_id=@did"
            : "a.doctor_id=@did AND a.status=@status";
        cmd.CommandText = $"""
SELECT a.*, p.name as patient_name, p.gender, p.age,
  (SELECT file_name FROM patient_word_docs WHERE admission_id=a.id LIMIT 1) as word_file
FROM admissions a JOIN patients p ON p.id=a.patient_id
WHERE {where} ORDER BY a.admission_date DESC
""";
        cmd.Parameters.AddWithValue("@did", doctorId);
        if (!string.IsNullOrEmpty(statusFilter))
            cmd.Parameters.AddWithValue("@status", statusFilter);
        var list = new List<Admission>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var adm = MapAdmission(r);
            adm.Patient = new Patient
            {
                Name = r["patient_name"].ToString()!,
                Gender = r["gender"].ToString()!,
                Age = Convert.ToInt32(r["age"])
            };
            adm.WordStatus = r["word_file"] == DBNull.Value ? "未创建" : "已创建";
            list.Add(adm);
        }
        return list;
    }

    public Admission? GetAdmissionById(int id)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT a.*, p.name as patient_name, p.gender, p.age FROM admissions a JOIN patients p ON p.id=a.patient_id WHERE a.id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var adm = MapAdmission(r);
        adm.Patient = new Patient { Name = r["patient_name"].ToString()!, Gender = r["gender"].ToString()!, Age = Convert.ToInt32(r["age"]) };
        return adm;
    }

    public void BindDoctorPatient(int doctorId, int admissionId)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO doctor_patient_map (doctor_id, admission_id) VALUES (@did, @aid)";
        cmd.Parameters.AddWithValue("@did", doctorId);
        cmd.Parameters.AddWithValue("@aid", admissionId);
        cmd.ExecuteNonQuery();
    }

    public void InsertInsuranceInfo(PatientInsuranceInfo info)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO patient_insurance_info (admission_id, insurance_type, insurance_region) VALUES (@aid, @type, @region)";
        cmd.Parameters.AddWithValue("@aid", info.AdmissionId);
        cmd.Parameters.AddWithValue("@type", info.InsuranceType);
        cmd.Parameters.AddWithValue("@region", info.InsuranceRegion);
        cmd.ExecuteNonQuery();
    }

    public List<Admission> Search(int doctorId, string keyword)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
SELECT a.*, p.name as patient_name, p.gender, p.age,
  (SELECT file_name FROM patient_word_docs WHERE admission_id=a.id LIMIT 1) as word_file
FROM admissions a JOIN patients p ON p.id=a.patient_id
WHERE a.doctor_id=@did AND (p.name LIKE @kw OR a.admission_no LIKE @kw OR a.main_diagnosis LIKE @kw OR a.bed_no LIKE @kw)
ORDER BY a.admission_date DESC
""";
        cmd.Parameters.AddWithValue("@did", doctorId);
        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
        var list = new List<Admission>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var adm = MapAdmission(r);
            adm.Patient = new Patient { Name = r["patient_name"].ToString()!, Gender = r["gender"].ToString()!, Age = Convert.ToInt32(r["age"]) };
            adm.WordStatus = r["word_file"] == DBNull.Value ? "未创建" : "已创建";
            list.Add(adm);
        }
        return list;
    }

    public void DeleteAdmission(int admissionId)
    {
        using var c = DbConnectionFactory.Create();
        // 删除关联数据
        // 先删子表（rehab_assessment_results 依赖 rehab_assessment_records）
        using var preCmd = c.CreateCommand();
        preCmd.CommandText = "DELETE FROM rehab_assessment_results WHERE record_id IN (SELECT id FROM rehab_assessment_records WHERE admission_id=@id)";
        preCmd.Parameters.AddWithValue("@id", admissionId);
        preCmd.ExecuteNonQuery();

        foreach (var sql in new[]
        {
            "DELETE FROM progress_notes WHERE admission_id=@id",
            "DELETE FROM rehab_assessment_records WHERE admission_id=@id",
            "DELETE FROM patient_word_docs WHERE admission_id=@id",
            "DELETE FROM patient_insurance_info WHERE admission_id=@id",
            "DELETE FROM doctor_patient_map WHERE admission_id=@id",
            "DELETE FROM research_case_index WHERE admission_id=@id",
            "DELETE FROM admissions WHERE id=@id",
        })
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@id", admissionId);
            cmd.ExecuteNonQuery();
        }
    }

    private static Patient MapPatient(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("id")),
        Name = r["name"].ToString()!,
        Gender = r["gender"].ToString()!,
        Age = Convert.ToInt32(r["age"]),
        Phone = r["phone"].ToString()!,
        AllergyHistory = r["allergy_history"].ToString()!,
        PastHistory = r["past_history"].ToString()!,
        Remark = r["remark"].ToString()!,
    };

    private static Admission MapAdmission(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("id")),
        PatientId = Convert.ToInt32(r["patient_id"]),
        DoctorId = Convert.ToInt32(r["doctor_id"]),
        AdmissionNo = r["admission_no"].ToString()!,
        BedNo = r["bed_no"].ToString()!,
        Department = r["department"].ToString()!,
        AdmissionDate = DateTime.Parse(r["admission_date"].ToString()!),
        DischargeDate = r["discharge_date"] == DBNull.Value ? null : DateTime.Parse(r["discharge_date"].ToString()!),
        MainDiagnosis = r["main_diagnosis"].ToString()!,
        SecondaryDiagnosis = r["secondary_diagnosis"].ToString()!,
        Status = r["status"].ToString()!,
        DischargeOutcome = r["discharge_outcome"].ToString()!,
        DischargeOrders = r["discharge_orders"].ToString()!,
        RehabAdvice = r["rehab_advice"].ToString()!,
        ExercisePrescription = r["exercise_prescription"].ToString()!,
        FollowUpAdvice = r["follow_up_advice"].ToString()!,
        ResearchNote = r["research_note"].ToString()!,
    };
}
