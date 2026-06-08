using BingChengAssistant.Data;
using BingChengAssistant.Models;
using ClosedXML.Excel;

namespace BingChengAssistant.Services;

public static class ResearchIndexService
{
    public static void Update(int admissionId)
    {
        var doctor = AppContextService.CurrentDoctor;
        if (doctor == null) return;

        var patRepo = new PatientRepository();
        var noteRepo = new ProgressNoteRepository();
        var rehabRepo = new RehabRepository();
        var wordRepo = new WordDocRepository();
        var idxRepo = new ResearchIndexRepository();

        var adm = patRepo.GetAdmissionById(admissionId);
        if (adm == null) return;

        var notes = noteRepo.GetByAdmission(admissionId);
        var rehabs = rehabRepo.GetByAdmission(admissionId);
        var wordDoc = wordRepo.GetByAdmission(admissionId);

        var idx = new ResearchCaseIndex
        {
            AdmissionId = admissionId,
            DoctorId = doctor.Id,
            PatientName = adm.Patient?.Name ?? "",
            AdmissionNo = adm.AdmissionNo,
            MainDiagnosis = adm.MainDiagnosis,
            AdmissionDate = adm.AdmissionDate,
            DischargeDate = adm.DischargeDate,
            DoctorName = doctor.Name,
            NoteCount = notes.Count,
            RehabCount = rehabs.Count,
            WordFilePath = wordDoc?.FilePath ?? "",
        };
        idxRepo.Upsert(idx);
        ExportToExcel(doctor);
    }

    private static void ExportToExcel(Doctor doctor)
    {
        try
        {
            var idxRepo = new ResearchIndexRepository();
            var list = idxRepo.GetByDoctor(doctor.Id);
            var excelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctors", doctor.FolderName, "research_index.xlsx");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("科研索引");
            ws.Cell(1, 1).Value = "姓名";
            ws.Cell(1, 2).Value = "住院号";
            ws.Cell(1, 3).Value = "主要诊断";
            ws.Cell(1, 4).Value = "入院日期";
            ws.Cell(1, 5).Value = "出院日期";
            ws.Cell(1, 6).Value = "主管医生";
            ws.Cell(1, 7).Value = "病程数";
            ws.Cell(1, 8).Value = "评估数";
            ws.Cell(1, 9).Value = "Word文件";

            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                ws.Cell(i + 2, 1).Value = r.PatientName;
                ws.Cell(i + 2, 2).Value = r.AdmissionNo;
                ws.Cell(i + 2, 3).Value = r.MainDiagnosis;
                ws.Cell(i + 2, 4).Value = r.AdmissionDate.ToString("yyyy-MM-dd");
                ws.Cell(i + 2, 5).Value = r.DischargeDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(i + 2, 6).Value = r.DoctorName;
                ws.Cell(i + 2, 7).Value = r.NoteCount;
                ws.Cell(i + 2, 8).Value = r.RehabCount;
                ws.Cell(i + 2, 9).Value = r.WordFilePath;
            }
            ws.Columns().AdjustToContents();
            wb.SaveAs(excelPath);
        }
        catch (Exception ex)
        {
            LogService.Error("导出科研索引失败", ex);
        }
    }
}
