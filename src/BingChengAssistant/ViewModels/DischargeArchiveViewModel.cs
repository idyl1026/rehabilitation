using BingChengAssistant.Data;
using BingChengAssistant.Models;
using BingChengAssistant.Services;

namespace BingChengAssistant.ViewModels;

public class DischargeArchiveViewModel : BaseViewModel
{
    private Admission? _admission;
    private DateTime _dischargeDate = DateTime.Today;
    private string _dischargeDiagnosis = "";
    private string _dischargeOutcome = "好转";
    private string _dischargeOrders = "";
    private string _rehabAdvice = "";
    private string _exercisePrescription = "";
    private string _followUpAdvice = "";
    private string _researchNote = "";
    private string _errorMessage = "";

    public Admission? Admission { get => _admission; set { SetField(ref _admission, value); if (value != null) LoadData(); } }
    public DateTime DischargeDate { get => _dischargeDate; set => SetField(ref _dischargeDate, value); }
    public string DischargeDiagnosis { get => _dischargeDiagnosis; set => SetField(ref _dischargeDiagnosis, value); }
    public string DischargeOutcome { get => _dischargeOutcome; set => SetField(ref _dischargeOutcome, value); }
    public string DischargeOrders { get => _dischargeOrders; set => SetField(ref _dischargeOrders, value); }
    public string RehabAdvice { get => _rehabAdvice; set => SetField(ref _rehabAdvice, value); }
    public string ExercisePrescription { get => _exercisePrescription; set => SetField(ref _exercisePrescription, value); }
    public string FollowUpAdvice { get => _followUpAdvice; set => SetField(ref _followUpAdvice, value); }
    public string ResearchNote { get => _researchNote; set => SetField(ref _researchNote, value); }
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public string[] OutcomeOptions { get; } = { "痊愈", "好转", "未愈", "转院", "死亡" };

    public Action? OnArchived { get; set; }
    public RelayCommand ArchiveCommand => new(Archive);

    private void LoadData()
    {
        if (_admission == null) return;
        DischargeDiagnosis = _admission.MainDiagnosis;
    }

    private void Archive()
    {
        ErrorMessage = "";
        if (Admission == null) { ErrorMessage = "请选择患者"; return; }
        if (string.IsNullOrWhiteSpace(DischargeDiagnosis)) { ErrorMessage = "请填写出院诊断"; return; }

        var doctor = AppContextService.CurrentDoctor!;
        var patRepo = new PatientRepository();

        // 更新入院记录
        Admission.DischargeDate = DischargeDate;
        Admission.MainDiagnosis = DischargeDiagnosis;
        Admission.Status = "已归档";
        Admission.DischargeOutcome = DischargeOutcome;
        Admission.DischargeOrders = DischargeOrders;
        Admission.RehabAdvice = RehabAdvice;
        Admission.ExercisePrescription = ExercisePrescription;
        Admission.FollowUpAdvice = FollowUpAdvice;
        Admission.ResearchNote = ResearchNote;
        patRepo.UpdateAdmission(Admission);

        // 生成出院归档Word
        var noteRepo = new ProgressNoteRepository();
        var rehabRepo = new RehabRepository();
        var notes = noteRepo.GetByAdmission(Admission.Id);
        var rehabs = rehabRepo.GetByAdmission(Admission.Id);

        var exportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctors", doctor.FolderName, "exports");
        Directory.CreateDirectory(exportDir);
        var exportFile = Path.Combine(exportDir, $"{Admission.AdmissionDate:yyyy-MM-dd}-{Admission.Patient?.Name}-出院归档.docx");

        try
        {
            WordDocumentService.GenerateDischargeDocument(exportFile, Admission, doctor, notes, rehabs);
            // 更新Word状态
            var wordRepo = new WordDocRepository();
            var doc = wordRepo.GetByAdmission(Admission.Id);
            if (doc != null) wordRepo.UpdateStatus(doc.Id, "已归档");
        }
        catch (Exception ex)
        {
            LogService.Error("生成归档Word失败", ex);
        }

        // 更新科研索引
        ResearchIndexService.Update(Admission.Id);
        OperationLogService.Log("出院归档", Admission.Patient?.Name);
        OnArchived?.Invoke();
    }
}
