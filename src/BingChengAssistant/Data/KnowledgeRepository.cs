using BingChengAssistant.Models;
using Microsoft.Data.Sqlite;

namespace BingChengAssistant.Data;

public class KnowledgeRepository
{
    public List<KnowledgeItem> GetAll()
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM knowledge_base WHERE is_active=1 ORDER BY category, id";
        return ReadList(cmd);
    }

    public List<KnowledgeItem> Search(string keyword)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM knowledge_base WHERE is_active=1 AND (title LIKE @kw OR content LIKE @kw OR tags LIKE @kw OR category LIKE @kw) ORDER BY category, id";
        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
        return ReadList(cmd);
    }

    public List<string> GetCategories()
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT category FROM knowledge_base WHERE is_active=1 ORDER BY category";
        var list = new List<string>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(r[0].ToString()!);
        return list;
    }

    public int Insert(KnowledgeItem item)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
INSERT INTO knowledge_base (title, content, category, tags)
VALUES (@title, @content, @cat, @tags);
SELECT last_insert_rowid();
""";
        cmd.Parameters.AddWithValue("@title", item.Title);
        cmd.Parameters.AddWithValue("@content", item.Content);
        cmd.Parameters.AddWithValue("@cat", item.Category);
        cmd.Parameters.AddWithValue("@tags", item.Tags);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void Update(KnowledgeItem item)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE knowledge_base SET title=@title, content=@content, category=@cat, tags=@tags WHERE id=@id";
        cmd.Parameters.AddWithValue("@title", item.Title);
        cmd.Parameters.AddWithValue("@content", item.Content);
        cmd.Parameters.AddWithValue("@cat", item.Category);
        cmd.Parameters.AddWithValue("@tags", item.Tags);
        cmd.Parameters.AddWithValue("@id", item.Id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE knowledge_base SET is_active=0 WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private static List<KnowledgeItem> ReadList(SqliteCommand cmd)
    {
        var list = new List<KnowledgeItem>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new KnowledgeItem
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Title = r["title"].ToString()!,
                Content = r["content"].ToString()!,
                Category = r["category"].ToString()!,
                Tags = r["tags"].ToString()!,
                IsActive = Convert.ToInt32(r["is_active"]) == 1,
            });
        return list;
    }
}
