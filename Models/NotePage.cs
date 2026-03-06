namespace HandwrittenNotes.Models;

public class NotePage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<InkStroke> Strokes { get; set; } = new List<InkStroke>();
    public int PageIndex { get; set; }
    public double Width { get; set; } = 794;
    public double Height { get; set; } = 1123;
    public string? BackgroundImagePath { get; set; }
}

