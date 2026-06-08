using BingChengAssistant.Models;
using Microsoft.Data.Sqlite;

namespace BingChengAssistant.Data;

public class DoctorRepository
{
    public bool HasAnyDoctor()
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM doctors WHERE is_active=1";
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    public List<Doctor> GetAll()
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM doctors WHERE is_active=1 ORDER BY is_default DESC, id ASC";
        var list = new List<Doctor>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public Doctor? GetById(int id)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM doctors WHERE id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public bool EmployeeNoExists(string no)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM doctors WHERE employee_no=@no";
        cmd.Parameters.AddWithValue("@no", no);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    public int Insert(Doctor d)
    {
        using var c = DbConnectionFactory.Create();
        // 如果是默认，清除其他默认
        if (d.IsDefault)
        {
            using var clr = c.CreateCommand();
            clr.CommandText = "UPDATE doctors SET is_default=0";
            clr.ExecuteNonQuery();
        }
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
INSERT INTO doctors (name, employee_no, department, title, pin_hash, is_default)
VALUES (@name, @no, @dept, @title, @pin, @def);
SELECT last_insert_rowid();
""";
        cmd.Parameters.AddWithValue("@name", d.Name);
        cmd.Parameters.AddWithValue("@no", d.EmployeeNo);
        cmd.Parameters.AddWithValue("@dept", d.Department);
        cmd.Parameters.AddWithValue("@title", d.Title);
        cmd.Parameters.AddWithValue("@pin", d.PinHash);
        cmd.Parameters.AddWithValue("@def", d.IsDefault ? 1 : 0);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void LogLogin(int doctorId)
    {
        using var c = DbConnectionFactory.Create();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO doctor_login_logs (doctor_id) VALUES (@id)";
        cmd.Parameters.AddWithValue("@id", doctorId);
        cmd.ExecuteNonQuery();
    }

    private static Doctor Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("id")),
        Name = r["name"].ToString()!,
        EmployeeNo = r["employee_no"].ToString()!,
        Department = r["department"].ToString()!,
        Title = r["title"].ToString()!,
        PinHash = r["pin_hash"].ToString()!,
        IsDefault = Convert.ToInt32(r["is_default"]) == 1,
        IsActive = Convert.ToInt32(r["is_active"]) == 1,
    };
}
