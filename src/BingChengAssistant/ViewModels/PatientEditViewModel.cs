using System.IO;
using BingChengAssistant.Data;
using BingChengAssistant.Models;
using BingChengAssistant.Services;

namespace BingChengAssistant.ViewModels;

public class PatientEditViewModel : BaseViewModel
{
    // Patient fields
    private string _name = "", _gender = "男", _phone = "", _allergyHistory = "", _pastHistory = "", _remark = "";
    private int _age;

    // Admission fields
    private string _admissionNo = "", _bedNo = "", _department = "", _mainDiagnosis = "", _secondaryDiagnosis = "";
    private string _insuranceType = "", _insuranceRegion = "";
    private DateTime _admissionDate = DateTime.Today;
    private string _errorMessage = "";

    public string Name { get => _name; set => SetField(ref _name, value); }
    public string Gender { get => _gender; set => SetField(ref _gender, value); }
    public int Age { get => _age; set => SetField(ref _age, value); }
    public string Phone { get => _phone; set => SetField(ref _phone, value); }
    public string AllergyHistory { get => _allergyHistory; set => SetField(ref _allergyHistory, value); }
    public string PastHistory { get => _pastHistory; set => SetField(ref _pastHistory, value); }
    public string Remark { get => _remark; set => SetField(ref _remark, value); }
    public string AdmissionNo { get => _admissionNo; set => SetField(ref _admissionNo, value); }
    public string BedNo { get => _bedNo; set => SetField(ref _bedNo, value); }
    public string Department { get => _department; set => SetField(ref _department, value); }
    public DateTime AdmissionDate { get => _admissionDate; set => SetField(ref _admissionDate, value); }
    public string MainDiagnosis { get => _mainDiagnosis; set => SetField(ref _mainDiagnosis, value); }
    public string SecondaryDiagnosis { get => _secondaryDiagnosis; set => SetField(ref _secondaryDiagnosis, value); }
    public string InsuranceType { get => _insuranceType; set => SetField(ref _insuranceType, value); }
    public string InsuranceRegion { get => _insuranceRegion; set => SetField(ref _insuranceRegion, value); }
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public bool IsEdit { get; private set; }
    private int _patientId, _admissionId;

    public Action? OnSuccess { get; set; }
    public RelayCommand SaveCommand => new(Save);

    public PatientEditViewModel() { }

    public void LoadAdmission(Admission adm)
    {
        IsEdit = true;
        _patientId = adm.PatientId;
        _admissionId = adm.Id;
        var p = adm.Patient!;
        Name = p.Name; Gender = p.Gender; Age = p.Age; Phone = p.Phone;
        AllergyHistory = p.AllergyHistory; PastHistory = p.PastHistory; Remark = p.Remark;
        AdmissionNo = adm.AdmissionNo; BedNo = adm.BedNo; Department = adm.Department;
        AdmissionDate = adm.AdmissionDate; MainDiagnosis = adm.MainDiagnosis;
        SecondaryDiagnosis = adm.SecondaryDiagnosis;
    }

    private void Save()
    {
        ErrorMessage = "";
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "患者姓名不能为空"; return; }
        if (string.IsNullOrWhiteSpace(MainDiagnosis)) { ErrorMessage = "主要诊断不能为空"; return; }

        var doctor = AppContextService.CurrentDoctor!;
        var repo = new PatientRepository();

        var patient = new Patient
        {
            Id = _patientId, Name = Name.Trim(), Gender = Gender, Age = Age,
            Phone = Phone, AllergyHistory = AllergyHistory, PastHistory = PastHistory, Remark = Remark
        };

        var adm = new Admission
        {
            Id = _admissionId, PatientId = _patientId, DoctorId = doctor.Id,
            AdmissionNo = AdmissionNo.Trim(), BedNo = BedNo.Trim(),
            Department = string.IsNullOrWhiteSpace(Department) ? doctor.Department : Department.Trim(),
            AdmissionDate = AdmissionDate, MainDiagnosis = MainDiagnosis.Trim(),
            SecondaryDiagnosis = SecondaryDiagnosis.Trim(), Status = "在院"
        };

        if (IsEdit)
        {
            repo.UpdatePatient(patient);
            repo.UpdateAdmission(adm);
        }
        else
        {
            int pid = repo.InsertPatient(patient);
            patient.Id = pid;
            adm.PatientId = pid;
            int aid = repo.InsertAdmission(adm);
            adm.Id = aid;

            // 绑定医患
            repo.BindDoctorPatient(doctor.Id, aid);

            // 保险信息
            if (!string.IsNullOrEmpty(InsuranceType) || !string.IsNullOrEmpty(InsuranceRegion))
                repo.InsertInsuranceInfo(new PatientInsuranceInfo { AdmissionId = aid, InsuranceType = InsuranceType, InsuranceRegion = InsuranceRegion });

            // 创建Word文档
            var wordPath = FilePathService.GetPatientWordPath(doctor.FolderName, AdmissionDate, Name.Trim(), AdmissionNo.Trim());
            adm.Patient = patient;
            try
            {
                WordDocumentService.CreatePatientDocument(wordPath, adm, doctor);
                var wordRepo = new WordDocRepository();
                wordRepo.Insert(new WordDocumentInfo
                {
                    AdmissionId = aid,
                    FilePath = wordPath,
                    FileName = Path.GetFileName(wordPath),
                    Status = "已创建"
                });
            }
            catch (Exception ex)
            {
                LogService.Error("创建Word失败", ex);
            }

            // 科研索引
            adm.Patient = patient;
            ResearchIndexService.Update(aid);
        }

        OperationLogService.Log(IsEdit ? "编辑患者" : "新建患者", Name);
        OnSuccess?.Invoke();
    }
}
