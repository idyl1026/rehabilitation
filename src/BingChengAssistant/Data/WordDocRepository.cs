using BingChengAssistant.Models;
using Microsoft.Data.Sqlite;

namespace BingChengAssistant.Data;

public class WordDocRepository
{
    public int Insert(WordDocumentInfo doc)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
INSERT INTO patient_word_docs (admission_id, file_path, file_name, status)
VALUES (@aid, @path, @name, @status);
SELECT last_insert_rowid();
""";
        cmd.Parameters.AddWithValue("@aid", doc.AdmissionId);
        cmd.Parameters.AddWithValue("@path", doc.FilePath);
        cmd.Parameters.AddWithValue("@name", doc.FileName);
        cmd.Parameters.AddWithValue("@status", doc.Status);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public WordDocumentInfo? GetByAdmission(int admissionId)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM patient_word_docs WHERE admission_id=@aid LIMIT 1";
        cmd.Parameters.AddWithValue("@aid", admissionId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new WordDocumentInfo
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            AdmissionId = Convert.ToInt32(r["admission_id"]),
            FilePath = r["file_path"].ToString()!,
            FileName = r["file_name"].ToString()!,
            Status = r["status"].ToString()!,
        };
    }

    public void UpdateSyncTime(int id)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE patient_word_docs SET last_synced_at=datetime('now','localtime') WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpdateStatus(int id, string status)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE patient_word_docs SET status=@s WHERE id=@id";
        cmd.Parameters.AddWithValue("@s", status);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
}

public class ResearchIndexRepository
{
    public void Upsert(ResearchCaseIndex idx)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
INSERT INTO research_case_index
  (admission_id, doctor_id, patient_name, admission_no, main_diagnosis, admission_date, discharge_date, doctor_name, note_count, rehab_count, word_file_path, research_note, updated_at)
VALUES (@aid, @did, @pname, @ano, @diag, @admdate, @disdate, @dname, @nc, @rc, @wpath, @research, datetime('now','localtime'))
ON CONFLICT(admission_id) DO UPDATE SET
  patient_name=excluded.patient_name, main_diagnosis=excluded.main_diagnosis,
  discharge_date=excluded.discharge_date, note_count=excluded.note_count,
  rehab_count=excluded.rehab_count, word_file_path=excluded.word_file_path,
  research_note=excluded.research_note, updated_at=excluded.updated_at;
""";
        // Add UNIQUE constraint workaround — use INSERT OR REPLACE
        cmd.CommandText = """
INSERT OR REPLACE INTO research_case_index
  (id, admission_id, doctor_id, patient_name, admission_no, main_diagnosis, admission_date, discharge_date, doctor_name, note_count, rehab_count, word_file_path, research_note, updated_at)
VALUES (
  (SELECT id FROM research_case_index WHERE admission_id=@aid),
  @aid, @did, @pname, @ano, @diag, @admdate, @disdate, @dname, @nc, @rc, @wpath, @research, datetime('now','localtime')
)
""";
        cmd.Parameters.AddWithValue("@aid", idx.AdmissionId);
        cmd.Parameters.AddWithValue("@did", idx.DoctorId);
        cmd.Parameters.AddWithValue("@pname", idx.PatientName);
        cmd.Parameters.AddWithValue("@ano", idx.AdmissionNo);
        cmd.Parameters.AddWithValue("@diag", idx.MainDiagnosis);
        cmd.Parameters.AddWithValue("@admdate", idx.AdmissionDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@disdate", idx.DischargeDate.HasValue ? idx.DischargeDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);
        cmd.Parameters.AddWithValue("@dname", idx.DoctorName);
        cmd.Parameters.AddWithValue("@nc", idx.NoteCount);
        cmd.Parameters.AddWithValue("@rc", idx.RehabCount);
        cmd.Parameters.AddWithValue("@wpath", idx.WordFilePath);
        cmd.Parameters.AddWithValue("@research", idx.ResearchNote);
        cmd.ExecuteNonQuery();
    }

    public List<ResearchCaseIndex> GetByDoctor(int doctorId)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM research_case_index WHERE doctor_id=@did ORDER BY updated_at DESC";
        cmd.Parameters.AddWithValue("@did", doctorId);
        var list = new List<ResearchCaseIndex>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new ResearchCaseIndex
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            AdmissionId = Convert.ToInt32(r["admission_id"]),
            PatientName = r["patient_name"].ToString()!,
            AdmissionNo = r["admission_no"].ToString()!,
            MainDiagnosis = r["main_diagnosis"].ToString()!,
            DoctorName = r["doctor_name"].ToString()!,
            NoteCount = Convert.ToInt32(r["note_count"]),
            RehabCount = Convert.ToInt32(r["rehab_count"]),
            WordFilePath = r["word_file_path"].ToString()!,
        });
        return list;
    }
}
