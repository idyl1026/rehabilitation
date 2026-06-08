using BingChengAssistant.Models;
using Microsoft.Data.Sqlite;

namespace BingChengAssistant.Data;

public class RehabRepository
{
    public List<RehabScaleDict> GetScales()
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM rehab_scale_dict WHERE is_active=1";
        var list = new List<RehabScaleDict>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new RehabScaleDict
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            Code = r["code"].ToString()!,
            Name = r["name"].ToString()!,
            Description = r["description"].ToString()!,
            ScaleType = r["scale_type"].ToString()!,
        });
        return list;
    }

    public int InsertRecord(RehabAssessmentRecord rec)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
INSERT INTO rehab_assessment_records
(admission_id, doctor_id, scale_id, scale_name, assessment_date, result_summary, interpretation, rehab_advice, note_text)
VALUES (@aid, @did, @sid, @sname, @date, @result, @interp, @advice, @note);
SELECT last_insert_rowid();
""";
        cmd.Parameters.AddWithValue("@aid", rec.AdmissionId);
        cmd.Parameters.AddWithValue("@did", rec.DoctorId);
        cmd.Parameters.AddWithValue("@sid", rec.ScaleId);
        cmd.Parameters.AddWithValue("@sname", rec.ScaleName);
        cmd.Parameters.AddWithValue("@date", rec.AssessmentDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@result", rec.ResultSummary);
        cmd.Parameters.AddWithValue("@interp", rec.Interpretation);
        cmd.Parameters.AddWithValue("@advice", rec.RehabAdvice);
        cmd.Parameters.AddWithValue("@note", rec.NoteText);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<RehabAssessmentRecord> GetByAdmission(int admissionId)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM rehab_assessment_records WHERE admission_id=@aid ORDER BY assessment_date DESC";
        cmd.Parameters.AddWithValue("@aid", admissionId);
        var list = new List<RehabAssessmentRecord>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new RehabAssessmentRecord
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            AdmissionId = Convert.ToInt32(r["admission_id"]),
            ScaleName = r["scale_name"].ToString()!,
            ResultSummary = r["result_summary"].ToString()!,
            Interpretation = r["interpretation"].ToString()!,
            NoteText = r["note_text"].ToString()!,
            AssessmentDate = DateTime.Parse(r["assessment_date"].ToString()!),
        });
        return list;
    }

    public void MarkSynced(int id)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE rehab_assessment_records SET is_synced_to_word=1 WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
}
