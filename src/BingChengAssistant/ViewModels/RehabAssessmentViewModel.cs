using System.IO;
using System.Collections.ObjectModel;
using System.Windows;
using BingChengAssistant.Data;
using BingChengAssistant.Models;
using BingChengAssistant.Services;

namespace BingChengAssistant.ViewModels;

public class RehabAssessmentViewModel : BaseViewModel
{
    private Admission? _admission;
    private RehabScaleDict? _selectedScale;
    private int _vasNrsScore = 0;
    private int _mmtGrade = 3;
    private string _mmtMuscle = "";
    private string _romJoint = "", _romDirection = "", _romActive = "", _romPassive = "", _romPain = "", _romRemark = "";
    private string _interpretation = "", _advice = "", _resultSummary = "", _noteText = "";

    public Admission? Admission { get => _admission; set => SetField(ref _admission, value); }

    public ObservableCollection<RehabScaleDict> Scales { get; } = new();

    public RehabScaleDict? SelectedScale
    {
        get => _selectedScale;
        set { SetField(ref _selectedScale, value); OnPropertyChanged(nameof(IsVasNrs)); OnPropertyChanged(nameof(IsMmt)); OnPropertyChanged(nameof(IsRom)); }
    }

    public bool IsVasNrs => SelectedScale?.Code is "VAS" or "NRS";
    public bool IsMmt => SelectedScale?.Code == "MMT";
    public bool IsRom => SelectedScale?.Code == "ROM";

    public int VasNrsScore { get => _vasNrsScore; set => SetField(ref _vasNrsScore, value); }
    public int MmtGrade { get => _mmtGrade; set => SetField(ref _mmtGrade, value); }
    public string MmtMuscle { get => _mmtMuscle; set => SetField(ref _mmtMuscle, value); }
    public string RomJoint { get => _romJoint; set => SetField(ref _romJoint, value); }
    public string RomDirection { get => _romDirection; set => SetField(ref _romDirection, value); }
    public string RomActive { get => _romActive; set => SetField(ref _romActive, value); }
    public string RomPassive { get => _romPassive; set => SetField(ref _romPassive, value); }
    public string RomPain { get => _romPain; set => SetField(ref _romPain, value); }
    public string RomRemark { get => _romRemark; set => SetField(ref _romRemark, value); }
    public string Interpretation { get => _interpretation; set => SetField(ref _interpretation, value); }
    public string Advice { get => _advice; set => SetField(ref _advice, value); }
    public string ResultSummary { get => _resultSummary; set => SetField(ref _resultSummary, value); }
    public string NoteText { get => _noteText; set => SetField(ref _noteText, value); }

    public Action? OnSaved { get; set; }
    public RelayCommand CalcCommand => new(Calculate);
    public RelayCommand SaveCommand => new(Save);
    public RelayCommand CopyNoteCommand => new(() => { if (!string.IsNullOrEmpty(NoteText)) Clipboard.SetText(NoteText); });

    public RehabAssessmentViewModel()
    {
        var repo = new RehabRepository();
        foreach (var s in repo.GetScales()) Scales.Add(s);
        SelectedScale = Scales.FirstOrDefault();
    }

    private void Calculate()
    {
        if (SelectedScale == null) return;

        if (IsVasNrs)
        {
            var (interp, advice) = RehabScoreService.InterpretVasNrs(VasNrsScore);
            ResultSummary = $"{VasNrsScore}分";
            Interpretation = interp;
            Advice = advice;
        }
        else if (IsMmt)
        {
            var (interp, advice) = RehabScoreService.InterpretMmt(MmtGrade);
            ResultSummary = $"{MmtGrade}级 - {MmtMuscle}";
            Interpretation = interp;
            Advice = advice;
        }
        else if (IsRom)
        {
            ResultSummary = $"{RomJoint} {RomDirection}：主动 {RomActive}°，被动 {RomPassive}°";
            Interpretation = $"关节活动度{(string.IsNullOrEmpty(RomPain) ? "正常" : $"伴{RomPain}")}";
            Advice = "继续关节活动度训练，注意在无痛范围内进行。";
        }

        NoteText = RehabScoreService.BuildNoteText(SelectedScale.Name, ResultSummary, Interpretation, Advice);
    }

    private void Save()
    {
        if (SelectedScale == null || Admission == null) return;
        if (string.IsNullOrEmpty(ResultSummary)) { Calculate(); }

        var rec = new RehabAssessmentRecord
        {
            AdmissionId = Admission.Id,
            DoctorId = AppContextService.CurrentDoctor!.Id,
            ScaleId = SelectedScale.Id,
            ScaleName = SelectedScale.Name,
            ResultSummary = ResultSummary,
            Interpretation = Interpretation,
            RehabAdvice = Advice,
            NoteText = NoteText,
        };

        var repo = new RehabRepository();
        repo.InsertRecord(rec);

        // 同步Word
        var wordRepo = new WordDocRepository();
        var doc = wordRepo.GetByAdmission(Admission.Id);
        if (doc != null && File.Exists(doc.FilePath))
        {
            WordDocumentService.AppendRehabRecord(doc.FilePath, rec);
            repo.MarkSynced(rec.Id);
        }

        ResearchIndexService.Update(Admission.Id);
        OperationLogService.Log("康复评估", $"{Admission.Patient?.Name} - {SelectedScale.Name}");
        OnSaved?.Invoke();
    }
}
