using Microsoft.EntityFrameworkCore;
using MedicalProgress.App.Models;

namespace MedicalProgress.App.Data;

public class AppDbContext : DbContext
{
    public DbSet<Patient> Patients { get; set; }
    public DbSet<ProgressRecord> ProgressRecords { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<DiseaseCategory> DiseaseCategories { get; set; }
    public DbSet<KnowledgeTemplate> KnowledgeTemplates { get; set; }
    public DbSet<ImportedDocument> ImportedDocuments { get; set; }
    public DbSet<StructuredExamResult> StructuredExamResults { get; set; }
    public DbSet<DepartmentProfile> DepartmentProfiles { get; set; }

    private readonly string _dbPath;

    public AppDbContext()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MedicalProgress");

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        _dbPath = Path.Combine(appDataPath, "medical_progress.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Gender).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ChiefComplaint).IsRequired().HasMaxLength(500);
            entity.Property(e => e.BedNumber).HasMaxLength(50);
            entity.Property(e => e.MedicalRecordNumber).HasMaxLength(50);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.AttendingDoctor).HasMaxLength(200);
            entity.Property(e => e.Diagnosis).HasMaxLength(100);
            entity.Property(e => e.DischargeDiagnosis).HasMaxLength(500);
            entity.Property(e => e.DischargeOrders).HasMaxLength(2000);
            entity.Property(e => e.PatientFolder).HasMaxLength(500);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.AdmissionDate);
            entity.HasIndex(e => e.IsDischarged);
            entity.HasIndex(e => e.BedNumber);
            entity.HasIndex(e => e.MedicalRecordNumber);
        });

        modelBuilder.Entity<ProgressRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => e.RecordDate);
            entity.HasIndex(e => e.PatientId);
            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.ProgressRecords)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FolderPath).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<DiseaseCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.SubjectId);
            entity.HasIndex(e => new { e.SubjectId, e.Code }).IsUnique();
            entity.HasOne(e => e.Subject)
                  .WithMany(s => s.Categories)
                  .HasForeignKey(e => e.SubjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TemplateType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => e.SubjectId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.Keywords);
            entity.HasIndex(e => e.Title);
            entity.HasOne(e => e.Subject)
                  .WithMany(s => s.Templates)
                  .HasForeignKey(e => e.SubjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportedDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceFilePath).HasMaxLength(260);
            entity.Property(e => e.SourceFileName).HasMaxLength(120);
            entity.Property(e => e.SourceType).HasMaxLength(20);
            entity.Property(e => e.DocumentType).HasMaxLength(50);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.ImportedAt);
            entity.HasIndex(e => e.DocumentType);
            entity.HasOne(e => e.Patient)
                  .WithMany()
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StructuredExamResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExamType).HasMaxLength(50);
            entity.Property(e => e.ReportName).HasMaxLength(120);
            entity.Property(e => e.ItemName).HasMaxLength(120);
            entity.Property(e => e.ResultValue).HasMaxLength(80);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.ReferenceRange).HasMaxLength(80);
            entity.Property(e => e.AbnormalFlag).HasMaxLength(20);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.ImportedDocumentId);
            entity.HasIndex(e => e.ExamDate);
            entity.HasIndex(e => e.ExamType);
            entity.HasIndex(e => e.ItemName);
            entity.HasOne(e => e.Patient)
                  .WithMany()
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ImportedDocument)
                  .WithMany(d => d.ExamResults)
                  .HasForeignKey(e => e.ImportedDocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DepartmentProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        base.OnModelCreating(modelBuilder);
    }

    public void EnsureDatabaseCreated()
    {
        Database.EnsureCreated();
    }
}
