using System.Collections.ObjectModel;
using BingChengAssistant.Data;
using BingChengAssistant.Models;
using BingChengAssistant.Services;

namespace BingChengAssistant.ViewModels;

public class MainWorkbenchViewModel : BaseViewModel
{
    private Admission? _selectedAdmission;
    private string _searchKeyword = "";
    private string _statusFilter = "";
    private string _doctorInfo = "";

    public ObservableCollection<Admission> Admissions { get; } = new();
    public ObservableCollection<ProgressNote> Notes { get; } = new();
    public ObservableCollection<RehabAssessmentRecord> Rehabs { get; } = new();

    public Admission? SelectedAdmission
    {
        get => _selectedAdmission;
        set
        {
            SetField(ref _selectedAdmission, value);
            LoadNotes();
            LoadRehabs();
        }
    }

    public string SearchKeyword
    {
        get => _searchKeyword;
        set { SetField(ref _searchKeyword, value); Search(); }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set { SetField(ref _statusFilter, value); LoadAdmissions(); }
    }

    public string DoctorInfo { get => _doctorInfo; set => SetField(ref _doctorInfo, value); }

    // Stats
    private int _inHospitalCount, _dischargedCount, _archivedCount;
    public int InHospitalCount { get => _inHospitalCount; set => SetField(ref _inHospitalCount, value); }
    public int DischargedCount { get => _dischargedCount; set => SetField(ref _dischargedCount, value); }
    public int ArchivedCount { get => _archivedCount; set => SetField(ref _archivedCount, value); }

    // Actions set by View
    public Action? OpenNewPatient { get; set; }
    public Action<Admission>? OpenEditPatient { get; set; }
    public Action<Admission>? OpenNewNote { get; set; }
    public Action<Admission>? OpenRehab { get; set; }
    public Action<Admission>? OpenDischarge { get; set; }
    public Action<Admission>? OpenWordFile { get; set; }
    public Action<Admission>? OpenPatientDir { get; set; }

    public Action<Admission>? ConfirmDeleteAdmission { get; set; }

    private RelayCommand? _newPatientCommand;
    private RelayCommand? _newNoteCommand;
    private RelayCommand? _editPatientCommand;
    private RelayCommand? _rehabCommand;
    private RelayCommand? _dischargeCommand;
    private RelayCommand? _openWordCommand;
    private RelayCommand? _openDirCommand;
    private RelayCommand? _deletePatientCommand;

    public RelayCommand NewPatientCommand => _newPatientCommand ??= new(() => OpenNewPatient?.Invoke());
    public RelayCommand NewNoteCommand => _newNoteCommand ??= new(() =>
    {
        if (SelectedAdmission == null) { System.Windows.MessageBox.Show("请先在左侧列表点击选中一位患者。", "提示"); return; }
        OpenNewNote?.Invoke(SelectedAdmission);
    });
    public RelayCommand EditPatientCommand => _editPatientCommand ??= new(() =>
    {
        if (SelectedAdmission == null) { System.Windows.MessageBox.Show("请先在左侧列表点击选中一位患者。", "提示"); return; }
        OpenEditPatient?.Invoke(SelectedAdmission);
    });
    public RelayCommand RehabCommand => _rehabCommand ??= new(() =>
    {
        if (SelectedAdmission == null) { System.Windows.MessageBox.Show("请先在左侧列表点击选中一位患者。", "提示"); return; }
        OpenRehab?.Invoke(SelectedAdmission);
    });
    public RelayCommand DischargeCommand => _dischargeCommand ??= new(() =>
    {
        if (SelectedAdmission == null) { System.Windows.MessageBox.Show("请先在左侧列表点击选中一位患者。", "提示"); return; }
        OpenDischarge?.Invoke(SelectedAdmission);
    });
    public RelayCommand OpenWordCommand => _openWordCommand ??= new(() =>
    {
        if (SelectedAdmission == null) { System.Windows.MessageBox.Show("请先在左侧列表点击选中一位患者。", "提示"); return; }
        OpenWordFile?.Invoke(SelectedAdmission);
    });
    public RelayCommand OpenDirCommand => _openDirCommand ??= new(() =>
    {
        if (SelectedAdmission == null) { System.Windows.MessageBox.Show("请先在左侧列表点击选中一位患者。", "提示"); return; }
        OpenPatientDir?.Invoke(SelectedAdmission);
    });
    public RelayCommand DeletePatientCommand => _deletePatientCommand ??= new(() =>
    {
        if (SelectedAdmission == null) { System.Windows.MessageBox.Show("请先在左侧列表点击选中一位患者。", "提示"); return; }
        ConfirmDeleteAdmission?.Invoke(SelectedAdmission);
    });

    public MainWorkbenchViewModel()
    {
        var d = AppContextService.CurrentDoctor;
        DoctorInfo = d == null ? "" : $"{d.Name}  |  {d.Department}  |  {d.Title}";
        LoadAdmissions();
    }

    public void LoadAdmissions()
    {
        var repo = new PatientRepository();
        var list = repo.GetAdmissionsByDoctor(
            AppContextService.CurrentDoctor?.Id ?? 0,
            StatusFilter);
        Admissions.Clear();
        foreach (var a in list) Admissions.Add(a);
        UpdateStats();
    }

    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchKeyword)) { LoadAdmissions(); return; }
        var repo = new PatientRepository();
        var list = repo.Search(AppContextService.CurrentDoctor?.Id ?? 0, SearchKeyword);
        Admissions.Clear();
        foreach (var a in list) Admissions.Add(a);
    }

    private void LoadNotes()
    {
        Notes.Clear();
        if (SelectedAdmission == null) return;
        var repo = new ProgressNoteRepository();
        foreach (var n in repo.GetByAdmission(SelectedAdmission.Id))
            Notes.Add(n);
    }

    private void LoadRehabs()
    {
        Rehabs.Clear();
        if (SelectedAdmission == null) return;
        var repo = new RehabRepository();
        foreach (var r in repo.GetByAdmission(SelectedAdmission.Id))
            Rehabs.Add(r);
    }

    private void UpdateStats()
    {
        var repo = new PatientRepository();
        InHospitalCount = repo.GetAdmissionsByDoctor(AppContextService.CurrentDoctor?.Id ?? 0, "在院").Count;
        DischargedCount = repo.GetAdmissionsByDoctor(AppContextService.CurrentDoctor?.Id ?? 0, "已出院").Count;
        ArchivedCount = repo.GetAdmissionsByDoctor(AppContextService.CurrentDoctor?.Id ?? 0, "已归档").Count;
    }
}
