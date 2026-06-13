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

    /// <summary>清空全部知识库（彻底删除，并清空引用历史），返回删除条数</summary>
    public int ClearAll()
    {
        using var c = DbConnectionFactory.Create();
        using var count = c.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM knowledge_base WHERE is_active=1";
        var n = Convert.ToInt32(count.ExecuteScalar());

        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM knowledge_base; DELETE FROM knowledge_usage;";
        cmd.ExecuteNonQuery();
        return n;
    }

    /// <summary>记录一次引用，用于"最近引用前置"</summary>
    public void RecordUsage(int knowledgeId)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
INSERT INTO knowledge_usage (knowledge_id, use_count, last_used_at)
VALUES (@id, 1, datetime('now','localtime'))
ON CONFLICT(knowledge_id) DO UPDATE SET
  use_count = use_count + 1,
  last_used_at = datetime('now','localtime')
""";
        cmd.Parameters.AddWithValue("@id", knowledgeId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>最近引用的知识卡片（按最后引用时间倒序）</summary>
    public List<KnowledgeItem> GetRecent(int limit = 20)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
SELECT k.* FROM knowledge_base k
JOIN knowledge_usage u ON u.knowledge_id = k.id
WHERE k.is_active = 1
ORDER BY u.last_used_at DESC
LIMIT @lim
""";
        cmd.Parameters.AddWithValue("@lim", limit);
        return ReadList(cmd);
    }

    public List<KnowledgeItem> GetByCategory(string category)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM knowledge_base WHERE is_active=1 AND category=@cat ORDER BY id";
        cmd.Parameters.AddWithValue("@cat", category);
        return ReadList(cmd);
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
