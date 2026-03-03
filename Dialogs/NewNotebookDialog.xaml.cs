using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HandwrittenNotes.Dialogs;

public sealed partial class NewNotebookDialog : ContentDialog
{
    public string NotebookName => NotebookNameTextBox.Text;
    public double PageWidth { get; private set; } = 794;
    public double PageHeight { get; private set; } = 1123;
    public string BackgroundType { get; private set; } = "grid";
    public string BackgroundColor { get; private set; } = "#FFFFFF";

    public NewNotebookDialog()
    {
        this.InitializeComponent();
        this.PrimaryButtonClick += NewNotebookDialog_PrimaryButtonClick;
    }

    private void NewNotebookDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var pageSize = PageSizeComboBox.SelectedIndex switch
        {
            0 => (794.0, 1123.0),  // A4
            1 => (1123.0, 1587.0), // A3
            2 => (816.0, 1056.0),  // Letter
            _ => (794.0, 1123.0)
        };
        
        PageWidth = pageSize.Item1;
        PageHeight = pageSize.Item2;

        BackgroundType = BackgroundTypeComboBox.SelectedIndex switch
        {
            0 => "grid",
            1 => "blank",
            2 => "lined",
            3 => "dotted",
            _ => "grid"
        };
    }
}