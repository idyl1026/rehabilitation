using System.Collections.ObjectModel;
using BingChengAssistant.Data;
using BingChengAssistant.Models;

namespace BingChengAssistant.ViewModels;

public class KnowledgeBaseViewModel : BaseViewModel
{
    private KnowledgeItem? _selectedItem;
    private string _searchKeyword = "";
    private string _editTitle = "";
    private string _editContent = "";
    private string _editCategory = "通用";
    private string _editTags = "";
    private string _errorMessage = "";
    private bool _isEditing;

    public ObservableCollection<KnowledgeItem> Items { get; } = new();
    public string[] Categories { get; } = { "通用", "康复", "疼痛", "运动", "内科", "骨科", "神经", "其他" };

    public KnowledgeItem? SelectedItem
    {
        get => _selectedItem;
        set { SetField(ref _selectedItem, value); if (value != null) LoadForEdit(value); }
    }

    public string SearchKeyword { get => _searchKeyword; set { SetField(ref _searchKeyword, value); Search(); } }
    public string EditTitle { get => _editTitle; set => SetField(ref _editTitle, value); }
    public string EditContent { get => _editContent; set => SetField(ref _editContent, value); }
    public string EditCategory { get => _editCategory; set => SetField(ref _editCategory, value); }
    public string EditTags { get => _editTags; set => SetField(ref _editTags, value); }
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }
    public bool IsEditing { get => _isEditing; set => SetField(ref _isEditing, value); }

    private RelayCommand? _newCommand, _saveCommand, _deleteCommand, _clearCommand;
    public RelayCommand NewCommand => _newCommand ??= new(NewItem);
    public RelayCommand SaveCommand => _saveCommand ??= new(Save);
    public RelayCommand DeleteCommand => _deleteCommand ??= new(Delete);
    public RelayCommand ClearCommand => _clearCommand ??= new(ClearEdit);

    public KnowledgeBaseViewModel() => LoadAll();

    private void LoadAll()
    {
        Items.Clear();
        foreach (var i in new KnowledgeRepository().GetAll()) Items.Add(i);
    }

    private void Search()
    {
        Items.Clear();
        var list = string.IsNullOrWhiteSpace(SearchKeyword)
            ? new KnowledgeRepository().GetAll()
            : new KnowledgeRepository().Search(SearchKeyword);
        foreach (var i in list) Items.Add(i);
    }

    private void LoadForEdit(KnowledgeItem item)
    {
        IsEditing = true;
        EditTitle = item.Title;
        EditContent = item.Content;
        EditCategory = item.Category;
        EditTags = item.Tags;
        ErrorMessage = "";
    }

    private void NewItem()
    {
        SelectedItem = null;
        IsEditing = true;
        EditTitle = "";
        EditContent = "";
        EditCategory = "通用";
        EditTags = "";
        ErrorMessage = "";
    }

    private void Save()
    {
        ErrorMessage = "";
        if (string.IsNullOrWhiteSpace(EditTitle)) { ErrorMessage = "标题不能为空"; return; }
        if (string.IsNullOrWhiteSpace(EditContent)) { ErrorMessage = "内容不能为空"; return; }

        var repo = new KnowledgeRepository();
        if (_selectedItem != null && _selectedItem.Id > 0)
        {
            _selectedItem.Title = EditTitle;
            _selectedItem.Content = EditContent;
            _selectedItem.Category = EditCategory;
            _selectedItem.Tags = EditTags;
            repo.Update(_selectedItem);
        }
        else
        {
            repo.Insert(new KnowledgeItem { Title = EditTitle, Content = EditContent, Category = EditCategory, Tags = EditTags });
        }
        LoadAll();
        IsEditing = false;
        ErrorMessage = "";
    }

    private void Delete()
    {
        if (_selectedItem == null || _selectedItem.Id == 0) return;
        new KnowledgeRepository().Delete(_selectedItem.Id);
        ClearEdit();
        LoadAll();
    }

    private void ClearEdit()
    {
        SelectedItem = null;
        IsEditing = false;
        EditTitle = ""; EditContent = ""; EditCategory = "通用"; EditTags = "";
        ErrorMessage = "";
    }
}
