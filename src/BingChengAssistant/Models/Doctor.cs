namespace BingChengAssistant.Models;

public class Doctor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string EmployeeNo { get; set; } = "";
    public string Department { get; set; } = "";
    public string Title { get; set; } = "";
    public string PinHash { get; set; } = "";   // SHA256 hash，空表示无PIN
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string DisplayName => $"{Name}（{Department}）";
    public string FolderName => $"{Name}_{EmployeeNo}";
}
