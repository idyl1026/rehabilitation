using Microsoft.EntityFrameworkCore;
using MedicalProgress.App.Data;
using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class DatabaseService
{
    private readonly AppDbContext _context;

    public DatabaseService()
    {
        _context = new AppDbContext();
        _context.EnsureDatabaseCreated();
        EnsureImportSchema();
    }

    public async Task<List<Patient>> GetAllPatientsAsync()
    {
        return await _context.Patients
            .OrderByDescending(p => p.AdmissionDate)
            .ToListAsync();
    }

    public async Task<Patient?> GetPatientByIdAsync(int id)
    {
        return await _context.Patients
            .Include(p => p.ProgressRecords)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Patient> CreatePatientAsync(Patient patient)
    {
        patient.CreatedAt = DateTime.Now;
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
        return patient;
    }

    public async Task<Patient> UpdatePatientAsync(Patient patient)
    {
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync();
        return patient;
    }

    public async Task DeletePatientAsync(int id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient != null)
        {
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<ProgressRecord>> GetPatientProgressRecordsAsync(int patientId)
    {
        return await _context.ProgressRecords
            .Where(pr => pr.PatientId == patientId)
            .OrderBy(pr => pr.RecordDate)
            .ToListAsync();
    }

    public async Task<ProgressRecord?> GetLatestProgressRecordAsync(int patientId)
    {
        return await _context.ProgressRecords
            .Where(pr => pr.PatientId == patientId)
            .OrderByDescending(pr => pr.RecordDate)
            .FirstOrDefaultAsync();
    }

    public async Task<ProgressRecord> CreateProgressRecordAsync(ProgressRecord record)
    {
        record.CreatedAt = DateTime.Now;
        _context.ProgressRecords.Add(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<ProgressRecord> UpdateProgressRecordAsync(ProgressRecord record)
    {
        _context.ProgressRecords.Update(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task DeleteProgressRecordAsync(int id)
    {
        var record = await _context.ProgressRecords.FindAsync(id);
        if (record != null)
        {
            _context.ProgressRecords.Remove(record);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ImportedDocument> SaveImportedDocumentAsync(ImportedDocument document, List<StructuredExamResult> results)
    {
        document.ImportedAt = DateTime.Now;
        document.IsReviewed = true;
        document.ReviewedAt = DateTime.Now;

        _context.ImportedDocuments.Add(document);
        await _context.SaveChangesAsync();

        foreach (var result in results)
        {
            result.ImportedDocumentId = document.Id;
            result.PatientId = document.PatientId;
            result.IsReviewed = true;
            result.CreatedAt = DateTime.Now;
        }

        _context.StructuredExamResults.AddRange(results);
        await _context.SaveChangesAsync();

        return document;
    }

    public async Task<List<ImportedDocument>> GetImportedDocumentsAsync(int patientId)
    {
        return await _context.ImportedDocuments
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.ImportedAt)
            .ToListAsync();
    }

    public async Task<List<StructuredExamResult>> GetPatientExamResultsAsync(int patientId)
    {
        return await _context.StructuredExamResults
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.ExamDate ?? r.CreatedAt)
            .ThenBy(r => r.ExamType)
            .ToListAsync();
    }

    public async Task<int> GetHospitalDaysAsync(int patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null) return 0;

        return (DateTime.Now - patient.AdmissionDate).Days + 1;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void EnsureImportSchema()
    {
        _context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS ImportedDocuments (
                Id INTEGER NOT NULL CONSTRAINT PK_ImportedDocuments PRIMARY KEY AUTOINCREMENT,
                PatientId INTEGER NULL,
                SourceFilePath TEXT NOT NULL DEFAULT '',
                SourceFileName TEXT NOT NULL DEFAULT '',
                SourceType TEXT NOT NULL DEFAULT 'Paste',
                DocumentType TEXT NOT NULL DEFAULT 'Unknown',
                RawText TEXT NOT NULL DEFAULT '',
                NormalizedText TEXT NOT NULL DEFAULT '',
                IsReviewed INTEGER NOT NULL DEFAULT 0,
                ImportedAt TEXT NOT NULL,
                ReviewedAt TEXT NULL,
                CONSTRAINT FK_ImportedDocuments_Patients_PatientId FOREIGN KEY (PatientId) REFERENCES Patients (Id) ON DELETE SET NULL
            );
            """);

        _context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS StructuredExamResults (
                Id INTEGER NOT NULL CONSTRAINT PK_StructuredExamResults PRIMARY KEY AUTOINCREMENT,
                PatientId INTEGER NULL,
                ImportedDocumentId INTEGER NOT NULL,
                ExamDate TEXT NULL,
                ExamType TEXT NOT NULL DEFAULT 'Unknown',
                ReportName TEXT NOT NULL DEFAULT '',
                ItemName TEXT NOT NULL DEFAULT '',
                ResultValue TEXT NOT NULL DEFAULT '',
                Unit TEXT NOT NULL DEFAULT '',
                ReferenceRange TEXT NOT NULL DEFAULT '',
                AbnormalFlag TEXT NOT NULL DEFAULT '',
                Conclusion TEXT NOT NULL DEFAULT '',
                RawLine TEXT NOT NULL DEFAULT '',
                IsReviewed INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                CONSTRAINT FK_StructuredExamResults_Patients_PatientId FOREIGN KEY (PatientId) REFERENCES Patients (Id) ON DELETE SET NULL,
                CONSTRAINT FK_StructuredExamResults_ImportedDocuments_ImportedDocumentId FOREIGN KEY (ImportedDocumentId) REFERENCES ImportedDocuments (Id) ON DELETE CASCADE
            );
            """);

        _context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS DepartmentProfiles (
                Id INTEGER NOT NULL CONSTRAINT PK_DepartmentProfiles PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                DiagnosisKeywords TEXT NOT NULL DEFAULT '',
                ExamKeywords TEXT NOT NULL DEFAULT '',
                ScaleKeywords TEXT NOT NULL DEFAULT '',
                TreatmentKeywords TEXT NOT NULL DEFAULT '',
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL
            );
            """);

        _context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ImportedDocuments_PatientId ON ImportedDocuments (PatientId);");
        _context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ImportedDocuments_ImportedAt ON ImportedDocuments (ImportedAt);");
        _context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ImportedDocuments_DocumentType ON ImportedDocuments (DocumentType);");
        _context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_StructuredExamResults_PatientId ON StructuredExamResults (PatientId);");
        _context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_StructuredExamResults_ImportedDocumentId ON StructuredExamResults (ImportedDocumentId);");
        _context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_StructuredExamResults_ExamDate ON StructuredExamResults (ExamDate);");
        _context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_StructuredExamResults_ExamType ON StructuredExamResults (ExamType);");
        _context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_StructuredExamResults_ItemName ON StructuredExamResults (ItemName);");
        _context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_DepartmentProfiles_Name ON DepartmentProfiles (Name);");
        _context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_DepartmentProfiles_IsActive ON DepartmentProfiles (IsActive);");
    }
}
