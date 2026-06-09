using System.IO;
using System.Collections.ObjectModel;
using System.Windows;
using BingChengAssistant.Data;
using BingChengAssistant.Models;
using BingChengAssistant.Services;
using System.Linq;

namespace BingChengAssistant.ViewModels;

public class ProgressNoteEditViewModel : BaseViewModel
{
    private string _noteType = "日常病程";
    private string _content = "";
    private NoteTemplate? _selectedTemplate;
    private Admission? _admission;

    public ObservableCollection<NoteTemplate> Templates { get; } = new();
    public string[] NoteTypes { get; } = { "首次病程", "日常病程", "上级查房", "康复评估", "出院前" };

    public Admission? Admission { get => _admission; set { SetField(ref _admission, value); } }
    public string NoteType
    {
        get => _noteType;
        set
        {
            SetField(ref _noteType, value);
            LoadTemplates();
        }
    }
    public string Content
    {
        get => _content;
        set { SetField(ref _content, value); OnPropertyChanged(nameof(WordCount)); }
    }
    public string WordCount => $"已输入 {_content.Length} 字";
    public NoteTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            SetField(ref _selectedTemplate, value);
            if (value != null && Admission != null)
                Content = TemplateRenderService.Render(value.Content, Admission);
        }
    }

    // 知识库搜索
    private string _knowledgeKeyword = "";
    private KnowledgeItem? _selectedKnowledge;
    public ObservableCollection<KnowledgeItem> KnowledgeResults { get; } = new();
    public KnowledgeItem? SelectedKnowledge { get => _selectedKnowledge; set => SetField(ref _selectedKnowledge, value); }
    public string KnowledgeKeyword
    {
        get => _knowledgeKeyword;
        set { SetField(ref _knowledgeKeyword, value); SearchKnowledge(); }
    }
    private void SearchKnowledge()
    {
        KnowledgeResults.Clear();
        var list = string.IsNullOrWhiteSpace(_knowledgeKeyword)
            ? new KnowledgeRepository().GetAll().Take(10).ToList()
            : new KnowledgeRepository().Search(_knowledgeKeyword);
        foreach (var k in list) KnowledgeResults.Add(k);
    }
    public void InsertKnowledge(KnowledgeItem item)
        => Content += $"\n\n【{item.Title}】\n{item.Content}";

    public bool IsEdit { get; private set; }
    private int _noteId;

    public void LoadNote(ProgressNote note)
    {
        IsEdit = true;
        _noteId = note.Id;
        NoteType = note.NoteType;
        Content = note.Content;
    }

    public Action? OnSaved { get; set; }

    private RelayCommand? _saveCommand;
    private RelayCommand? _copyCommand;
    private RelayCommand? _syncWordCommand;
    public RelayCommand SaveCommand => _saveCommand ??= new(Save);
    public RelayCommand CopyCommand => _copyCommand ??= new(CopyToClipboard);
    public RelayCommand SyncWordCommand => _syncWordCommand ??= new(SyncWord);

    public ProgressNoteEditViewModel()
    {
        LoadTemplates();
        SearchKnowledge();
    }

    private void LoadTemplates()
    {
        Templates.Clear();
        var repo = new TemplateRepository();
        var list = string.IsNullOrEmpty(_noteType)
            ? repo.GetAll()
            : repo.GetByType(_noteType);
        // 如果该类型没有模板，回退到全部
        if (list.Count == 0) list = repo.GetAll();
        foreach (var t in list) Templates.Add(t);
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Content)) return;
        var repo = new ProgressNoteRepository();
        if (IsEdit)
        {
            repo.Update(new ProgressNote { Id = _noteId, AdmissionId = Admission!.Id, DoctorId = AppContextService.CurrentDoctor!.Id, NoteType = NoteType, Content = Content, RecordDate = DateTime.Now });
        }
        else
        {
            var note = new ProgressNote { AdmissionId = Admission!.Id, DoctorId = AppContextService.CurrentDoctor!.Id, NoteType = NoteType, Content = Content, RecordDate = DateTime.Now };
            repo.Insert(note);
        }
        ResearchIndexService.Update(Admission!.Id);
        OperationLogService.Log(IsEdit ? "编辑病程" : "保存病程", $"{Admission.Patient?.Name} - {NoteType}");
        OnSaved?.Invoke();
    }

    private void CopyToClipboard()
    {
        if (!string.IsNullOrEmpty(Content))
        {
            Clipboard.SetText(Content);
        }
    }

    private void SyncWord()
    {
        if (string.IsNullOrWhiteSpace(Content) || Admission == null) return;
        var wordRepo = new WordDocRepository();
        var doc = wordRepo.GetByAdmission(Admission.Id);
        if (doc == null || !File.Exists(doc.FilePath)) return;

        var note = new ProgressNote
        {
            AdmissionId = Admission.Id,
            NoteType = NoteType,
            Content = Content,
            RecordDate = DateTime.Now,
        };
        WordDocumentService.AppendProgressNote(doc.FilePath, note);
        wordRepo.UpdateSyncTime(doc.Id);
        OperationLogService.Log("同步Word", Admission.Patient?.Name);
    }
}
