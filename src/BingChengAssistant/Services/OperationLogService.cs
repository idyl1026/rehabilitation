using BingChengAssistant.Data;

namespace BingChengAssistant.Services;

public static class OperationLogService
{
    public static void Log(string operation, string detail = "")
    {
        try
        {
            using var c = DbConnectionFactory.Create();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO operation_logs (doctor_id, operation, detail) VALUES (@did, @op, @detail)";
            cmd.Parameters.AddWithValue("@did", AppContextService.CurrentDoctor?.Id ?? 0);
            cmd.Parameters.AddWithValue("@op", operation);
            cmd.Parameters.AddWithValue("@detail", detail);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }
}
