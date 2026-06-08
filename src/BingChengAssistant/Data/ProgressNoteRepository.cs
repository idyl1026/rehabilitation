using BingChengAssistant.Models;
using Microsoft.Data.Sqlite;

namespace BingChengAssistant.Data;

public class ProgressNoteRepository
{
    public int Insert(ProgressNote n)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
INSERT INTO progress_notes (admission_id, doctor_id, note_type, content, record_date)
VALUES (@aid, @did, @type, @content, @date);
SELECT last_insert_rowid();
""";
        cmd.Parameters.AddWithValue("@aid", n.AdmissionId);
        cmd.Parameters.AddWithValue("@did", n.DoctorId);
        cmd.Parameters.AddWithValue("@type", n.NoteType);
        cmd.Parameters.AddWithValue("@content", n.Content);
        cmd.Parameters.AddWithValue("@date", n.RecordDate.ToString("yyyy-MM-dd HH:mm:ss"));
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void Update(ProgressNote n)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE progress_notes SET note_type=@type, content=@content, record_date=@date, is_synced_to_word=@synced WHERE id=@id";
        cmd.Parameters.AddWithValue("@type", n.NoteType);
        cmd.Parameters.AddWithValue("@content", n.Content);
        cmd.Parameters.AddWithValue("@date", n.RecordDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@synced", n.IsSyncedToWord ? 1 : 0);
        cmd.Parameters.AddWithValue("@id", n.Id);
        cmd.ExecuteNonQuery();
    }

    public List<ProgressNote> GetByAdmission(int admissionId)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM progress_notes WHERE admission_id=@aid ORDER BY record_date DESC";
        cmd.Parameters.AddWithValue("@aid", admissionId);
        var list = new List<ProgressNote>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public void MarkSynced(int id)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE progress_notes SET is_synced_to_word=1 WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private static ProgressNote Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("id")),
        AdmissionId = Convert.ToInt32(r["admission_id"]),
        DoctorId = Convert.ToInt32(r["doctor_id"]),
        NoteType = r["note_type"].ToString()!,
        Content = r["content"].ToString()!,
        RecordDate = DateTime.Parse(r["record_date"].ToString()!),
        IsSyncedToWord = Convert.ToInt32(r["is_synced_to_word"]) == 1,
    };
}

public class TemplateRepository
{
    public List<NoteTemplate> GetAll()
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM note_templates WHERE is_active=1 ORDER BY is_built_in DESC, id ASC";
        var list = new List<NoteTemplate>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public int Insert(NoteTemplate t)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO note_templates (name, note_type, content, is_built_in) VALUES (@n, @type, @content, @built); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@n", t.Name);
        cmd.Parameters.AddWithValue("@type", t.NoteType);
        cmd.Parameters.AddWithValue("@content", t.Content);
        cmd.Parameters.AddWithValue("@built", t.IsBuiltIn ? 1 : 0);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void Update(NoteTemplate t)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE note_templates SET name=@n, note_type=@type, content=@content WHERE id=@id";
        cmd.Parameters.AddWithValue("@n", t.Name);
        cmd.Parameters.AddWithValue("@type", t.NoteType);
        cmd.Parameters.AddWithValue("@content", t.Content);
        cmd.Parameters.AddWithValue("@id", t.Id);
        cmd.ExecuteNonQuery();
    }

    private static NoteTemplate Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("id")),
        Name = r["name"].ToString()!,
        NoteType = r["note_type"].ToString()!,
        Content = r["content"].ToString()!,
        IsBuiltIn = Convert.ToInt32(r["is_built_in"]) == 1,
    };
}
