using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HandwrittenNotes.Models;
using HandwrittenNotes.ViewModels;
using HandwrittenNotes.Services;
using HandwrittenNotes.Dialogs;
using Windows.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI;

namespace HandwrittenNotes;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private System.Threading.Timer? _autoSaveTimer;
    private string _notebookSearchKeyword = string.Empty;

    public MainWindow()
    {
        this.InitializeComponent();

        // 设置窗口大小和标题
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));
        this.Title = "📝 手写笔记";

        // 自定义标题栏颜色
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = this.AppWindow.TitleBar;
            titleBar.BackgroundColor = Color.FromArgb(255, 24, 24, 37);
            titleBar.ForegroundColor = Color.FromArgb(255, 205, 214, 244);
            titleBar.InactiveBackgroundColor = Color.FromArgb(255, 24, 24, 37);
            titleBar.InactiveForegroundColor = Color.FromArgb(255, 150, 150, 170);
            titleBar.ButtonBackgroundColor = Color.FromArgb(255, 24, 24, 37);
            titleBar.ButtonForegroundColor = Color.FromArgb(255, 205, 214, 244);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 49, 50, 68);
            titleBar.ButtonHoverForegroundColor = Colors.White;
        }

        this.Activated += MainWindow_Activated;
        this.Closed += MainWindow_Closed;
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        // 只在第一次激活时加载
        this.Activated -= MainWindow_Activated;
        await _viewModel.InitializeAsync();
        RefreshNotebookList();

        if (_viewModel.CurrentNotebook != null)
        {
            SelectCurrentNotebook();
        }

        // 启动自动保存（每30秒）
        _autoSaveTimer = new System.Threading.Timer(
            async _ => await AutoSaveAsync(),
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _autoSaveTimer?.Dispose();
        // 同步保存
        SaveCurrentPage();
        _viewModel.SaveCurrentNotebookAsync().GetAwaiter().GetResult();
    }

#region 笔记本管理

    private async void NewNotebookButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewNotebookDialog
        {
            XamlRoot = this.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var firstPage = new NotePage
            {
                PageIndex = 0,
                Width = dialog.PageWidth,
                Height = dialog.PageHeight,
                Strokes = NotebookTemplateService.BuildTemplateStrokes(
                    dialog.TemplateType,
                    dialog.PageWidth,
                    dialog.PageHeight)
            };

            var notebook = new Notebook
            {
                Name = dialog.NotebookName,
                PageWidth = dialog.PageWidth,
                PageHeight = dialog.PageHeight,
                BackgroundType = dialog.BackgroundType,
                BackgroundColor = dialog.BackgroundColor,
                Pages = [firstPage]
            };

            _viewModel.Notebooks.Insert(0, notebook);
            _viewModel.SelectNotebook(notebook);
            await _viewModel.SaveCurrentNotebookAsync();
            
            RefreshNotebookList();
            SelectCurrentNotebook();
        }
    }

    private void NotebookListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NotebookListView.SelectedItem is Notebook notebook)
        {
            // 先保存当前页面
            SaveCurrentPage();
            _viewModel.SelectNotebook(notebook);
            LoadCurrentPage();
        }
    }

    private async void DeleteNotebook_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
        {
            var notebook = _viewModel.Notebooks.FirstOrDefault(n => n.Id == id);
            if (notebook == null) return;

            var dialog = new ContentDialog
            {
                Title = "确认删除",
                Content = $"确定要删除笔记「{notebook.Name}」吗？此操作不可恢复。",
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await _viewModel.DeleteNotebookAsync(notebook);
                RefreshNotebookList();
                if (_viewModel.CurrentNotebook != null)
                {
                    SelectCurrentNotebook();
                }
                else
                {
                    ShowWelcomePanel();
                }
            }
        }
    }

    private void RefreshNotebookList()
    {
        var keyword = _notebookSearchKeyword.Trim();
        var notebooks = string.IsNullOrWhiteSpace(keyword)
            ? _viewModel.Notebooks
            : _viewModel.Notebooks
                .Where(n => n.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

        var visibleCount = notebooks.Count();
        NotebookListView.ItemsSource = null;
        NotebookListView.ItemsSource = notebooks;

        NotebookCountText.Text = $"{_viewModel.Notebooks.Count} 本笔记";
        NotebookEmptyState.Visibility = visibleCount == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (visibleCount == 0)
        {
            var isSearchMode = !string.IsNullOrWhiteSpace(keyword);
            NotebookEmptyTitleText.Text = isSearchMode ? "没有匹配的笔记" : "还没有笔记";
            NotebookEmptyDescriptionText.Text = isSearchMode
                ? "试试其他关键词"
                : "点击上方按钮创建第一本笔记";
        }
    }

    private void SelectCurrentNotebook()
    {
        RefreshNotebookList();
        NotebookListView.SelectedItem = _viewModel.CurrentNotebook;
        LoadCurrentPage();
        ShowCanvasPanel();
    }

    private void NotebookSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _notebookSearchKeyword = NotebookSearchBox.Text ?? string.Empty;
        RefreshNotebookList();
    }

    #endregion

#region 页面管理

    private async Task LoadCurrentPage()
    {
        var page = _viewModel.CurrentPage;
        var notebook = _viewModel.CurrentNotebook;

        if (page != null && notebook != null)
        {
            var pageWidth = page.Width > 0 ? page.Width : notebook.PageWidth;
            var pageHeight = page.Height > 0 ? page.Height : notebook.PageHeight;
            
            DrawingCanvas.SetCanvasSize((float)pageWidth, (float)pageHeight);
            DrawingCanvas.SetBackgroundStyle(notebook.BackgroundColor, notebook.BackgroundType);
            DrawingCanvas.LoadStrokes(page.Strokes);

            if (!string.IsNullOrEmpty(page.BackgroundImagePath) && File.Exists(page.BackgroundImagePath))
            {
                await DrawingCanvas.LoadBackgroundImageAsync(page.BackgroundImagePath);
            }
            else
            {
                DrawingCanvas.ClearBackground();
            }
        }
        UpdatePageInfo();
    }

    private void SaveCurrentPage()
    {
        if (_viewModel.CurrentPage != null)
        {
            _viewModel.UpdateCurrentPageStrokes(DrawingCanvas.GetStrokes());
        }
    }

private async void PrevPageButton_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentPage();
        _viewModel.PreviousPage();
        await LoadCurrentPage();
    }

    private async void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentPage();
        _viewModel.NextPage();
        await LoadCurrentPage();
    }

private async void AddPageButton_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentPage();
        _viewModel.AddPage();
        await LoadCurrentPage();
    }

    private void UpdatePageInfo()
    {
        PageInfoText.Text = _viewModel.PageInfo;
    }

    #endregion

    #region 工具栏事件

    private void ToolBar_ToolChanged(object? sender, InkToolType tool)
    {
        DrawingCanvas.CurrentTool = tool;
    }

    private void ToolBar_ColorChanged(object? sender, Color color)
    {
        DrawingCanvas.StrokeColor = color;
    }

    private void ToolBar_StrokeWidthChanged(object? sender, float width)
    {
        DrawingCanvas.StrokeWidth = width;
    }

    private void ToolBar_UndoRequested(object? sender, EventArgs e)
    {
        DrawingCanvas.Undo();
    }

    private void ToolBar_RedoRequested(object? sender, EventArgs e)
    {
        DrawingCanvas.Redo();
    }

private void ToolBar_ClearRequested(object? sender, EventArgs e)
    {
        DrawingCanvas.Clear();
    }

    private async void ToolBar_ExportPdfRequested(object? sender, EventArgs e)
    {
        if (_viewModel.CurrentNotebook == null) return;

        var service = new PdfExportService();
        var success = await service.ExportToPdfAsync(_viewModel.CurrentNotebook);

        if (success)
        {
            var dialog = new ContentDialog
            {
                Title = "导出成功",
                Content = "笔记已成功导出为PDF文件。",
                CloseButtonText = "确定",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private async void ToolBar_ImportPdfRequested(object? sender, EventArgs e)
    {
        var service = new PdfImportService();
        var file = await PdfImportService.PickPdfFileAsync();
        if (file == null)
        {
            return;
        }

        var (success, error, notebook, needPassword) = await service.ImportPdfFromStorageFileAsync(file, _viewModel);

        if (success && notebook != null)
        {
            RefreshNotebookList();
            SelectCurrentNotebook();
        }
        else if (needPassword)
        {
            var passwordBox = new PasswordBox
            {
                PlaceholderText = "请输入PDF密码",
                Width = 300
            };

            var dialog = new ContentDialog
            {
                Title = "PDF需要密码",
                Content = passwordBox,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = this.Content.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var password = passwordBox.Password;
                (success, error, notebook, _) = await service.ImportPdfFromStorageFileAsync(file, _viewModel, password);

                if (success && notebook != null)
                {
                    RefreshNotebookList();
                    SelectCurrentNotebook();
                }
                else if (!string.IsNullOrEmpty(error))
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "导入失败",
                        Content = $"导入PDF时出错：{error}",
                        CloseButtonText = "确定",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }
        else if (!string.IsNullOrEmpty(error))
        {
            var dialog = new ContentDialog
            {
                Title = "导入失败",
                Content = $"导入PDF时出错：{error}",
                CloseButtonText = "确定",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private async void ToolBar_InsertImageRequested(object? sender, EventArgs e)
    {
        if (_viewModel.CurrentPage == null) return;

        var service = new ImageImportService();
        var (imagePath, width, height) = await service.PickAndImportImageAsync();
        if (string.IsNullOrEmpty(imagePath)) return;

        _viewModel.CurrentPage.BackgroundImagePath = imagePath;
        if (width > 0 && height > 0)
        {
            _viewModel.CurrentPage.Width = width;
            _viewModel.CurrentPage.Height = height;
        }

        await LoadCurrentPage();
        await _viewModel.SaveCurrentNotebookAsync();
    }

    private void ToolBar_ZoomInRequested(object? sender, EventArgs e)
    {
        DrawingCanvas.ZoomIn();
        ToolBar.UpdateZoomLevel(DrawingCanvas.Zoom);
    }

    private void ToolBar_ZoomOutRequested(object? sender, EventArgs e)
    {
        DrawingCanvas.ZoomOut();
        ToolBar.UpdateZoomLevel(DrawingCanvas.Zoom);
    }

    private void ToolBar_ZoomResetRequested(object? sender, EventArgs e)
    {
        DrawingCanvas.ResetZoom();
        ToolBar.UpdateZoomLevel(DrawingCanvas.Zoom);
    }

    private void DrawingCanvas_ZoomChanged(object? sender, float zoom)
    {
        ToolBar.UpdateZoomLevel(zoom);
    }

    #endregion

    #region 画布事件

    private void DrawingCanvas_StrokesChanged(object? sender, EventArgs e)
    {
        SaveCurrentPage();
    }

    #endregion

    #region UI 状态

    private void ShowCanvasPanel()
    {
        WelcomePanel.Visibility = Visibility.Collapsed;
        DrawingCanvas.Visibility = Visibility.Visible;
        ToolBar.Visibility = Visibility.Visible;
        PageNavBar.Visibility = Visibility.Visible;
    }

    private void ShowWelcomePanel()
    {
        WelcomePanel.Visibility = Visibility.Visible;
        DrawingCanvas.Visibility = Visibility.Collapsed;
        ToolBar.Visibility = Visibility.Collapsed;
        PageNavBar.Visibility = Visibility.Collapsed;
    }

    #endregion

    #region 自动保存

    private async Task AutoSaveAsync()
    {
        try
        {
            DispatcherQueue.TryEnqueue(() => SaveCurrentPage());
            await _viewModel.SaveCurrentNotebookAsync();
        }
        catch
        {
            // 自动保存失败时静默忽略
        }
    }

    #endregion
}
