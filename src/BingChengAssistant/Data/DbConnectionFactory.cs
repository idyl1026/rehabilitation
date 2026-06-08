using System.IO;
using Microsoft.Data.Sqlite;

namespace BingChengAssistant.Data;

public static class DbConnectionFactory
{
    public static string DbPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "main.db");
    public static string ConnectionString => $"Data Source={DbPath}";

    public static SqliteConnection Create()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        // 启用外键约束
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        cmd.ExecuteNonQuery();
        return conn;
    }
}
