using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandwrittenNotes.Models;
using HandwrittenNotes.Services;
using System.Collections.ObjectModel;

namespace HandwrittenNotes.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly NotebookStorageService _storageService = new();

    [ObservableProperty]
    private ObservableCollection<Notebook> _notebooks = new ObservableCollection<Notebook>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPage))]
    [NotifyPropertyChangedFor(nameof(HasNotebook))]
    [NotifyPropertyChangedFor(nameof(PageInfo))]
    private Notebook? _currentNotebook;

    [ObservableProperty]
    private bool _isSaving;

    public NotePage? CurrentPage =>
        CurrentNotebook?.Pages.ElementAtOrDefault(CurrentNotebook.CurrentPageIndex);

    public bool HasNotebook => CurrentNotebook != null;

    public string PageInfo =>
        CurrentNotebook != null
            ? $"第 {CurrentNotebook.CurrentPageIndex + 1} / {CurrentNotebook.Pages.Count} 页"
            : "";

    public async Task InitializeAsync()
    {
        var notebooks = await _storageService.LoadAllNotebooksAsync();
        Notebooks = new ObservableCollection<Notebook>(notebooks);

        if (Notebooks.Count > 0)
        {
            CurrentNotebook = Notebooks[0];
        }
    }

    [RelayCommand]
    public async Task NewNotebookAsync()
    {
        var notebook = new Notebook
        {
            Name = $"笔记 {DateTime.Now:MM-dd HH:mm}"
        };
        Notebooks.Insert(0, notebook);
        CurrentNotebook = notebook;
        await SaveCurrentNotebookAsync();
    }

    [RelayCommand]
    public void SelectNotebook(Notebook notebook)
    {
        CurrentNotebook = notebook;
        OnPropertyChanged(nameof(CurrentPage));
        OnPropertyChanged(nameof(PageInfo));
    }

[RelayCommand]
    public Task DeleteNotebookAsync(Notebook notebook)
    {
        _storageService.DeleteNotebook(notebook.Id);
        Notebooks.Remove(notebook);
        if (CurrentNotebook == notebook)
        {
            CurrentNotebook = Notebooks.FirstOrDefault();
        }
        return Task.CompletedTask;
    }

[RelayCommand]
    public void AddPage()
    {
        if (CurrentNotebook == null) return;
        
        var pageWidth = CurrentNotebook.PageWidth > 0 ? CurrentNotebook.PageWidth : 1200;
        var pageHeight = CurrentNotebook.PageHeight > 0 ? CurrentNotebook.PageHeight : 1600;
        
        var newPage = new NotePage 
        { 
            PageIndex = CurrentNotebook.Pages.Count,
            Width = pageWidth,
            Height = pageHeight
        };
        CurrentNotebook.Pages.Add(newPage);
        CurrentNotebook.CurrentPageIndex = newPage.PageIndex;
        OnPropertyChanged(nameof(CurrentPage));
        OnPropertyChanged(nameof(PageInfo));
    }

    [RelayCommand]
    public void PreviousPage()
    {
        if (CurrentNotebook == null || CurrentNotebook.CurrentPageIndex <= 0) return;
        CurrentNotebook.CurrentPageIndex--;
        OnPropertyChanged(nameof(CurrentPage));
        OnPropertyChanged(nameof(PageInfo));
    }

    [RelayCommand]
    public void NextPage()
    {
        if (CurrentNotebook == null) return;
        if (CurrentNotebook.CurrentPageIndex >= CurrentNotebook.Pages.Count - 1) return;
        CurrentNotebook.CurrentPageIndex++;
        OnPropertyChanged(nameof(CurrentPage));
        OnPropertyChanged(nameof(PageInfo));
    }

    public void UpdateCurrentPageStrokes(List<InkStroke> strokes)
    {
        if (CurrentPage == null) return;
        CurrentPage.Strokes = strokes;
    }

    [RelayCommand]
    public async Task SaveCurrentNotebookAsync()
    {
        if (CurrentNotebook == null) return;
        IsSaving = true;
        try
        {
            await _storageService.SaveNotebookAsync(CurrentNotebook);
        }
        finally
        {
            IsSaving = false;
        }
    }
}

