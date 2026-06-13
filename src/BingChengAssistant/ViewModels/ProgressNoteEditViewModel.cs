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

    public Admission? Admission
    {
        get => _admission;
        set { SetField(ref _admission, value); OnAdmissionSet(); }
    }

    public string NoteType
    {
        get => _noteType;
        set { SetField(ref _noteType, value); LoadTemplates(); }
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

    // ===== 结构化录入字段（病程不含查体）=====
    private string _chief = "", _present = "", _auxExam = "", _assessment = "";
    public string ChiefComplaint { get => _chief; set => SetField(ref _chief, value); }
    public string PresentIllness { get => _present; set => SetField(ref _present, value); }
    public string AuxExam { get => _auxExam; set => SetField(ref _auxExam, value); }
    public string Assessment { get => _assessment; set => SetField(ref _assessment, value); }

    // ===== 知识库（分类标签 + 最近引用 + 搜索）=====
    private string _knowledgeKeyword = "";
    private string _selectedCategory = "最近引用";
    private KnowledgeItem? _selectedKnowledge;
    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<KnowledgeItem> KnowledgeResults { get; } = new();
    public KnowledgeItem? SelectedKnowledge { get => _selectedKnowledge; set => SetField(ref _selectedKnowledge, value); }
    public string SelectedCategory
    {
        get => _selectedCategory;
        set { SetField(ref _selectedCategory, value); SearchKnowledge(); }
    }
    public string KnowledgeKeyword
    {
        get => _knowledgeKeyword;
        set { SetField(ref _knowledgeKeyword, value); SearchKnowledge(); }
    }

    public Action? OnSaved { get; set; }
    public bool IsEdit { get; private set; }
    private int _noteId;

    private RelayCommand? _saveCommand, _copyCommand, _syncWordCommand, _composeCommand, _carryForwardCommand;
    public RelayCommand SaveCommand => _saveCommand ??= new(Save);
    public RelayCommand CopyCommand => _copyCommand ??= new(CopyToClipboard);
    public RelayCommand SyncWordCommand => _syncWordCommand ??= new(SyncWord);
    public RelayCommand ComposeCommand => _composeCommand ??= new(Compose);
    public RelayCommand CarryForwardCommand => _carryForwardCommand ??= new(CarryForward);

    public ProgressNoteEditViewModel()
    {
        MatchedCards.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasMatchedCards));
        LoadTemplates();
        LoadCategories();
        SearchKnowledge();
    }

    private void OnAdmissionSet()
    {
        if (_admission == null || IsEdit) return;
        // 新建病程：自动带入上次主诉/现病史；无既往则用首次病程模板
        CarryForward();
    }

    private void CarryForward()
    {
        if (_admission == null) return;
        var (chief, present) = NoteComposeService.CarryForward(_admission.Id);
        if (chief == "" && present == "")
        {
            // 无既往病程 → 取首次病程模板渲染填入
            var first = new TemplateRepository().GetByType("首次病程").FirstOrDefault()
                        ?? new TemplateRepository().GetAll().FirstOrDefault();
            if (first != null)
            {
                var rendered = TemplateRenderService.Render(first.Content, _admission);
                ChiefComplaint = NoteComposeService.ExtractSection(rendered, "主诉");
                PresentIllness = NoteComposeService.ExtractSection(rendered, "现病史");
            }
        }
        else
        {
            ChiefComplaint = chief;
            PresentIllness = present;
        }
    }

    /// <summary>一键整理：结构化字段 + 匹配知识卡片 → 标准病程全文</summary>
    // 本次匹配的卡片（可更换/移除）与候选池
    public ObservableCollection<KnowledgeItem> MatchedCards { get; } = new();
    private List<KnowledgeItem> _candidatePool = new();
    public bool HasMatchedCards => MatchedCards.Count > 0;

    private void Compose()
    {
        if (_admission == null) return;
        // 取较大候选池，前5张作为本次选用
        _candidatePool = NoteComposeService.MatchKnowledge(_admission.MainDiagnosis, 15);
        MatchedCards.Clear();
        foreach (var k in _candidatePool.Take(5)) MatchedCards.Add(k);
        ComposeFromMatched();
    }

    /// <summary>用当前 MatchedCards 组装病程全文</summary>
    private void ComposeFromMatched()
    {
        if (_admission == null) return;
        Content = NoteComposeService.Compose(
            _admission, AppContextService.CurrentDoctor, NoteType,
            ChiefComplaint, PresentIllness, AuxExam, Assessment, MatchedCards);
        foreach (var k in MatchedCards) new KnowledgeRepository().RecordUsage(k.Id);
    }

    /// <summary>更换某张匹配卡片为下一个候选；无候选则移除。返回是否成功换入新卡</summary>
    public void ReplaceMatchedCard(KnowledgeItem card)
    {
        int idx = MatchedCards.IndexOf(card);
        if (idx < 0) return;
        var next = _candidatePool.FirstOrDefault(c => !MatchedCards.Contains(c));
        if (next != null) MatchedCards[idx] = next;
        else MatchedCards.RemoveAt(idx);
        ComposeFromMatched();
    }

    public void RemoveMatchedCard(KnowledgeItem card)
    {
        if (MatchedCards.Remove(card)) ComposeFromMatched();
    }

    public void LoadNote(ProgressNote note)
    {
        IsEdit = true;
        _noteId = note.Id;
        NoteType = note.NoteType;
        Content = note.Content;
        // 编辑模式下把已有内容拆回结构化字段，便于二次整理
        ChiefComplaint = NoteComposeService.ExtractSection(note.Content, "主诉");
        PresentIllness = NoteComposeService.ExtractSection(note.Content, "现病史");
        AuxExam = NoteComposeService.ExtractSection(note.Content, "辅助检查");
        Assessment = NoteComposeService.ExtractSection(note.Content, "康复评估");
    }

    /// <summary>从康复评估窗口带回的评估文本，追加到评估字段</summary>
    public void AppendAssessment(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Assessment = string.IsNullOrWhiteSpace(Assessment) ? text : Assessment + "\n" + text;
    }

    private void LoadTemplates()
    {
        Templates.Clear();
        var repo = new TemplateRepository();
        var list = string.IsNullOrEmpty(_noteType) ? repo.GetAll() : repo.GetByType(_noteType);
        if (list.Count == 0) list = repo.GetAll();
        foreach (var t in list) Templates.Add(t);
    }

    private void LoadCategories()
    {
        Categories.Clear();
        Categories.Add("最近引用");
        Categories.Add("全部");
        foreach (var c in new KnowledgeRepository().GetCategories()) Categories.Add(c);
    }

    private void SearchKnowledge()
    {
        KnowledgeResults.Clear();
        var repo = new KnowledgeRepository();
        List<KnowledgeItem> list;
        if (!string.IsNullOrWhiteSpace(_knowledgeKeyword))
        {
            list = repo.Search(_knowledgeKeyword);
            if (_selectedCategory is not ("最近引用" or "全部"))
                list = list.Where(k => k.Category == _selectedCategory).ToList();
        }
        else if (_selectedCategory == "最近引用")
        {
            list = repo.GetRecent(20);
            if (list.Count == 0) list = repo.GetAll().Take(15).ToList(); // 无历史时给点默认
        }
        else if (_selectedCategory == "全部")
        {
            list = repo.GetAll().Take(50).ToList();
        }
        else
        {
            list = repo.GetByCategory(_selectedCategory);
        }
        foreach (var k in list) KnowledgeResults.Add(k);
    }

    /// <summary>知识卡片插入到病程时的标准格式文本</summary>
    public static string CardText(KnowledgeItem item) => $"\n\n【{item.Title}】\n{item.Content}";

    /// <summary>记录卡片被引用（用于最近引用前置）</summary>
    public void RecordKnowledgeUsed(int id)
    {
        new KnowledgeRepository().RecordUsage(id);
        if (_selectedCategory == "最近引用") SearchKnowledge();
    }

    /// <summary>供界面调用执行一键整理</summary>
    public void RunCompose() => Compose();

    private void Save()
    {
        // 未整理时若全文为空但有结构化内容，先整理
        if (string.IsNullOrWhiteSpace(Content) &&
            !(string.IsNullOrWhiteSpace(ChiefComplaint) && string.IsNullOrWhiteSpace(PresentIllness) && string.IsNullOrWhiteSpace(AuxExam)))
            Compose();
        if (string.IsNullOrWhiteSpace(Content)) return;

        var repo = new ProgressNoteRepository();
        if (IsEdit)
            repo.Update(new ProgressNote { Id = _noteId, AdmissionId = Admission!.Id, DoctorId = AppContextService.CurrentDoctor!.Id, NoteType = NoteType, Content = Content, RecordDate = DateTime.Now });
        else
            repo.Insert(new ProgressNote { AdmissionId = Admission!.Id, DoctorId = AppContextService.CurrentDoctor!.Id, NoteType = NoteType, Content = Content, RecordDate = DateTime.Now });

        ResearchIndexService.Update(Admission!.Id);
        OperationLogService.Log(IsEdit ? "编辑病程" : "保存病程", $"{Admission.Patient?.Name} - {NoteType}");
        OnSaved?.Invoke();
    }

    private void CopyToClipboard()
    {
        if (!string.IsNullOrEmpty(Content)) Clipboard.SetText(Content);
    }

    private void SyncWord()
    {
        if (string.IsNullOrWhiteSpace(Content) || Admission == null) return;
        var wordRepo = new WordDocRepository();
        var doc = wordRepo.GetByAdmission(Admission.Id);
        if (doc == null || !File.Exists(doc.FilePath)) return;
        var note = new ProgressNote { AdmissionId = Admission.Id, NoteType = NoteType, Content = Content, RecordDate = DateTime.Now };
        WordDocumentService.AppendProgressNote(doc.FilePath, note);
        wordRepo.UpdateSyncTime(doc.Id);
        OperationLogService.Log("同步Word", Admission.Patient?.Name);
    }
}
