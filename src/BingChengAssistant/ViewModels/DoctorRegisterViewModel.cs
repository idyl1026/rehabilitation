using BingChengAssistant.Data;
using BingChengAssistant.Models;
using BingChengAssistant.Services;

namespace BingChengAssistant.ViewModels;

public class DoctorRegisterViewModel : BaseViewModel
{
    private string _name = "";
    private string _employeeNo = "";
    private string _department = "康复运动医学科";
    private string _title = "";
    private string _pin = "";
    private string _confirmPin = "";
    private bool _isDefault = true;
    private string _errorMessage = "";

    public string Name { get => _name; set => SetField(ref _name, value); }
    public string EmployeeNo { get => _employeeNo; set => SetField(ref _employeeNo, value); }
    public string Department { get => _department; set => SetField(ref _department, value); }
    public string Title { get => _title; set => SetField(ref _title, value); }
    public string Pin { get => _pin; set => SetField(ref _pin, value); }
    public string ConfirmPin { get => _confirmPin; set => SetField(ref _confirmPin, value); }
    public bool IsDefault { get => _isDefault; set => SetField(ref _isDefault, value); }
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public Action? OnSuccess { get; set; }

    public RelayCommand RegisterCommand => new(Register);

    private void Register()
    {
        ErrorMessage = "";
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "请填写医生姓名"; return; }
        if (string.IsNullOrWhiteSpace(EmployeeNo)) { ErrorMessage = "请填写工号"; return; }
        if (!string.IsNullOrEmpty(Pin) && Pin != ConfirmPin) { ErrorMessage = "两次PIN不一致"; return; }

        var repo = new DoctorRepository();
        if (repo.EmployeeNoExists(EmployeeNo)) { ErrorMessage = $"工号 {EmployeeNo} 已存在"; return; }

        var doctor = new Doctor
        {
            Name = Name.Trim(),
            EmployeeNo = EmployeeNo.Trim(),
            Department = Department.Trim(),
            Title = Title.Trim(),
            PinHash = HashHelper.Sha256(Pin),
            IsDefault = IsDefault,
        };
        int id = repo.Insert(doctor);
        doctor.Id = id;

        // 创建目录
        Data.DirectoryInitializer.DoctorDir(doctor.FolderName);

        // 初始化科研索引Excel
        var excelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctors", doctor.FolderName, "research_index.xlsx");
        if (!File.Exists(excelPath))
        {
            try
            {
                using var wb = new ClosedXML.Excel.XLWorkbook();
                var ws = wb.Worksheets.Add("科研索引");
                ws.Cell(1, 1).Value = "姓名"; ws.Cell(1, 2).Value = "住院号";
                ws.Cell(1, 3).Value = "主要诊断"; ws.Cell(1, 4).Value = "入院日期";
                ws.Cell(1, 5).Value = "出院日期"; ws.Cell(1, 6).Value = "主管医生";
                ws.Cell(1, 7).Value = "病程数"; ws.Cell(1, 8).Value = "评估数";
                wb.SaveAs(excelPath);
            }
            catch { }
        }

        LogService.Info($"新医生注册：{doctor.Name}（{doctor.EmployeeNo}）");
        OperationLogService.Log("医生注册", doctor.Name);

        AppContextService.CurrentDoctor = doctor;
        OnSuccess?.Invoke();
    }
}
