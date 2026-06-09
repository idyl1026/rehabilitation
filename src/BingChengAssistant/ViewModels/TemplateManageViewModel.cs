using System.Collections.ObjectModel;
using BingChengAssistant.Data;
using BingChengAssistant.Models;

namespace BingChengAssistant.ViewModels;

public class TemplateManageViewModel : BaseViewModel
{
    private NoteTemplate? _selectedTemplate;
    private string _editName = "";
    private string _editNoteType = "日常病程";
    private string _editContent = "";
    private string _errorMessage = "";

    public ObservableCollection<NoteTemplate> Templates { get; } = new();
    public string[] NoteTypes { get; } = { "首次病程", "日常病程", "上级查房", "康复评估", "出院前" };

    public NoteTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set { SetField(ref _selectedTemplate, value); if (value != null) LoadForEdit(value); }
    }
    public string EditName { get => _editName; set => SetField(ref _editName, value); }
    public string EditNoteType { get => _editNoteType; set => SetField(ref _editNoteType, value); }
    public string EditContent { get => _editContent; set => SetField(ref _editContent, value); }
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    private RelayCommand? _newCommand, _saveCommand, _deleteCommand;
    public RelayCommand NewCommand => _newCommand ??= new(NewTemplate);
    public RelayCommand SaveCommand => _saveCommand ??= new(Save);
    public RelayCommand DeleteCommand => _deleteCommand ??= new(Delete);

    public TemplateManageViewModel() => LoadAll();

    private void LoadAll()
    {
        Templates.Clear();
        foreach (var t in new TemplateRepository().GetAll()) Templates.Add(t);
    }

    private void LoadForEdit(NoteTemplate t)
    {
        EditName = t.Name;
        EditNoteType = t.NoteType;
        EditContent = t.Content;
        ErrorMessage = "";
    }

    private void NewTemplate()
    {
        SelectedTemplate = null;
        EditName = ""; EditNoteType = "日常病程"; EditContent = ""; ErrorMessage = "";
    }

    private void Save()
    {
        ErrorMessage = "";
        if (string.IsNullOrWhiteSpace(EditName)) { ErrorMessage = "模板名称不能为空"; return; }
        if (string.IsNullOrWhiteSpace(EditContent)) { ErrorMessage = "模板内容不能为空"; return; }

        var repo = new TemplateRepository();
        if (_selectedTemplate != null && _selectedTemplate.Id > 0)
        {
            _selectedTemplate.Name = EditName;
            _selectedTemplate.NoteType = EditNoteType;
            _selectedTemplate.Content = EditContent;
            repo.Update(_selectedTemplate);
        }
        else
        {
            repo.Insert(new NoteTemplate { Name = EditName, NoteType = EditNoteType, Content = EditContent, IsBuiltIn = false });
        }
        LoadAll();
    }

    private void Delete()
    {
        if (_selectedTemplate == null) return;
        if (_selectedTemplate.IsBuiltIn)
        { ErrorMessage = "内置模板不能删除"; return; }
        new TemplateRepository().Delete(_selectedTemplate.Id);
        NewTemplate();
        LoadAll();
    }
}
