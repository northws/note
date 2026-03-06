namespace HandwrittenNotes.Models;

public class Notebook
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "未命名笔记";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    public List<NotePage> Pages { get; set; } = new List<NotePage> { new NotePage { PageIndex = 0 } };
    public int CurrentPageIndex { get; set; }
    public bool IsPdfNotebook { get; set; }
    public string? PdfFilePath { get; set; }
    public double PageWidth { get; set; } = 1200;
    public double PageHeight { get; set; } = 1600;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string BackgroundType { get; set; } = "grid"; // grid, blank, lined, dotted
}

