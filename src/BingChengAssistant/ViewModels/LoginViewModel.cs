using System.Collections.ObjectModel;
using BingChengAssistant.Data;
using BingChengAssistant.Models;
using BingChengAssistant.Services;

namespace BingChengAssistant.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private Doctor? _selectedDoctor;
    private string _pin = "";
    private string _errorMessage = "";

    public ObservableCollection<Doctor> Doctors { get; } = new();
    public Doctor? SelectedDoctor { get => _selectedDoctor; set => SetField(ref _selectedDoctor, value); }
    public string Pin { get => _pin; set => SetField(ref _pin, value); }
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public Action? OnLoginSuccess { get; set; }
    public Action? OnAddDoctor { get; set; }

    private RelayCommand? _loginCommand;
    private RelayCommand? _addDoctorCommand;
    public RelayCommand LoginCommand => _loginCommand ??= new(Login);
    public RelayCommand AddDoctorCommand => _addDoctorCommand ??= new(() => OnAddDoctor?.Invoke());

    public LoginViewModel()
    {
        LoadDoctors();
    }

    private void LoadDoctors()
    {
        Doctors.Clear();
        var repo = new DoctorRepository();
        foreach (var d in repo.GetAll())
            Doctors.Add(d);
        SelectedDoctor = Doctors.FirstOrDefault(d => d.IsDefault) ?? Doctors.FirstOrDefault();
    }

    private void Login()
    {
        ErrorMessage = "";
        if (SelectedDoctor == null) { ErrorMessage = "请选择医生"; return; }

        // 验证PIN
        if (!string.IsNullOrEmpty(SelectedDoctor.PinHash))
        {
            if (string.IsNullOrEmpty(Pin)) { ErrorMessage = "该医生已设置PIN，请输入"; return; }
            if (!HashHelper.Verify(Pin, SelectedDoctor.PinHash)) { ErrorMessage = "PIN不正确"; return; }
        }

        var repo = new DoctorRepository();
        repo.LogLogin(SelectedDoctor.Id);
        AppContextService.CurrentDoctor = SelectedDoctor;
        LogService.Info($"医生登录：{SelectedDoctor.Name}");
        OnLoginSuccess?.Invoke();
    }
}
