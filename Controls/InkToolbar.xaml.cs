using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using HandwrittenNotes.Models;
using Windows.UI;

namespace HandwrittenNotes.Controls;

public sealed partial class InkToolbar : UserControl
{
    // 事件
    public event EventHandler<InkToolType>? ToolChanged;
    public event EventHandler<Color>? ColorChanged;
    public event EventHandler<float>? StrokeWidthChanged;
    public event EventHandler? UndoRequested;
    public event EventHandler? RedoRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler? ExportPdfRequested;
    public event EventHandler? ImportPdfRequested;
    public event EventHandler? InsertImageRequested;
    public event EventHandler? ZoomInRequested;
    public event EventHandler? ZoomOutRequested;
    public event EventHandler? ZoomResetRequested;

    private InkToolType _currentTool = InkToolType.Pen;
    private Button? _selectedColorButton;

    public InkToolbar()
    {
        this.InitializeComponent();
        this.Loaded += InkToolbar_Loaded;
    }

    private void InkToolbar_Loaded(object sender, RoutedEventArgs e)
    {
        _selectedColorButton = ColorBlack;
        UpdateColorButtonBorders();
    }

    private void PenButton_Click(object sender, RoutedEventArgs e)
    {
        SetTool(InkToolType.Pen);
    }

    private void HighlighterButton_Click(object sender, RoutedEventArgs e)
    {
        SetTool(InkToolType.Highlighter);
    }

    private void EraserButton_Click(object sender, RoutedEventArgs e)
    {
        SetTool(InkToolType.Eraser);
    }

    private void SetTool(InkToolType tool)
    {
        _currentTool = tool;
        PenButton.IsChecked = tool == InkToolType.Pen;
        HighlighterButton.IsChecked = tool == InkToolType.Highlighter;
        EraserButton.IsChecked = tool == InkToolType.Eraser;
        ToolChanged?.Invoke(this, tool);
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            var color = ParseHexColor(hex);
            _selectedColorButton = btn;
            UpdateColorButtonBorders();
            ColorChanged?.Invoke(this, color);

            // 选择颜色时自动切换回笔工具
            if (_currentTool == InkToolType.Eraser)
            {
                SetTool(InkToolType.Pen);
            }
        }
    }

    private void UpdateColorButtonBorders()
    {
        var allButtons = new[] { ColorBlack, ColorRed, ColorBlue, ColorGreen, ColorOrange, ColorPurple };
        foreach (var btn in allButtons)
        {
            btn.BorderThickness = btn == _selectedColorButton
                ? new Thickness(2)
                : new Thickness(0);
            btn.BorderBrush = btn == _selectedColorButton
                ? new SolidColorBrush(Color.FromArgb(255, 180, 180, 180))
                : null;
        }
    }

    private void StrokeWidthSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        var width = (float)e.NewValue;
        if (StrokeWidthText != null)
        {
            StrokeWidthText.Text = width.ToString("F1");
        }
        StrokeWidthChanged?.Invoke(this, width);
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        UndoRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RedoButton_Click(object sender, RoutedEventArgs e)
    {
        RedoRequested?.Invoke(this, EventArgs.Empty);
    }

private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
    {
        ExportPdfRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ImportPdfButton_Click(object sender, RoutedEventArgs e)
    {
        ImportPdfRequested?.Invoke(this, EventArgs.Empty);
    }

    private void InsertImageButton_Click(object sender, RoutedEventArgs e)
    {
        InsertImageRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        ZoomInRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        ZoomOutRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ZoomResetButton_Click(object sender, RoutedEventArgs e)
    {
        ZoomResetRequested?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateZoomLevel(float zoom)
    {
        ZoomLevelText.Text = $"{(int)(zoom * 100)}%";
    }

    private static Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        byte r = Convert.ToByte(hex[..2], 16);
        byte g = Convert.ToByte(hex[2..4], 16);
        byte b = Convert.ToByte(hex[4..6], 16);
        return Color.FromArgb(255, r, g, b);
    }
}
