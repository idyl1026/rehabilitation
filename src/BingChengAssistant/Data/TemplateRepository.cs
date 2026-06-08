using BingChengAssistant.Models;
using Microsoft.Data.Sqlite;

namespace BingChengAssistant.Data;

public class TemplateRepository
{
    public List<NoteTemplate> GetAll()
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM note_templates WHERE is_active=1 ORDER BY id";
        var list = new List<NoteTemplate>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new NoteTemplate
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Name = r["name"].ToString()!,
                NoteType = r["note_type"].ToString()!,
                Content = r["content"].ToString()!,
                IsBuiltIn = Convert.ToInt32(r["is_built_in"]) == 1,
                IsActive = Convert.ToInt32(r["is_active"]) == 1,
            });
        }
        return list;
    }

    public List<NoteTemplate> GetByType(string noteType)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM note_templates WHERE is_active=1 AND note_type=@type ORDER BY id";
        cmd.Parameters.AddWithValue("@type", noteType);
        var list = new List<NoteTemplate>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new NoteTemplate
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Name = r["name"].ToString()!,
                NoteType = r["note_type"].ToString()!,
                Content = r["content"].ToString()!,
                IsBuiltIn = Convert.ToInt32(r["is_built_in"]) == 1,
                IsActive = Convert.ToInt32(r["is_active"]) == 1,
            });
        }
        return list;
    }

    public int Insert(NoteTemplate t)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
INSERT INTO note_templates (name, note_type, content, is_built_in)
VALUES (@name, @type, @content, @builtin);
SELECT last_insert_rowid();
""";
        cmd.Parameters.AddWithValue("@name", t.Name);
        cmd.Parameters.AddWithValue("@type", t.NoteType);
        cmd.Parameters.AddWithValue("@content", t.Content);
        cmd.Parameters.AddWithValue("@builtin", t.IsBuiltIn ? 1 : 0);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void Update(NoteTemplate t)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE note_templates SET name=@name, note_type=@type, content=@content WHERE id=@id";
        cmd.Parameters.AddWithValue("@name", t.Name);
        cmd.Parameters.AddWithValue("@type", t.NoteType);
        cmd.Parameters.AddWithValue("@content", t.Content);
        cmd.Parameters.AddWithValue("@id", t.Id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        // 只能删除非内置模板
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE note_templates SET is_active=0 WHERE id=@id AND is_built_in=0";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
}
