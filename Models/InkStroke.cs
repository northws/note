using System.Text.Json.Serialization;
using Windows.UI;

namespace HandwrittenNotes.Models;

/// <summary>
/// 单个墨水点，包含位置和压力信息
/// </summary>
public class InkPoint
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Pressure { get; set; } = 0.5f;

    public InkPoint() { }

    public InkPoint(float x, float y, float pressure = 0.5f)
    {
        X = x;
        Y = y;
        Pressure = pressure;
    }
}

/// <summary>
/// 笔触工具类型
/// </summary>
public enum InkToolType
{
    Pen,
    Highlighter,
    Eraser
}

/// <summary>
/// 单条墨水笔触
/// </summary>
public class InkStroke
{
    public List<InkPoint> Points { get; set; } = new List<InkPoint>();
    public byte ColorR { get; set; } = 0;
    public byte ColorG { get; set; } = 0;
    public byte ColorB { get; set; } = 0;
    public byte ColorA { get; set; } = 255;
    public float StrokeWidth { get; set; } = 2f;
    public InkToolType ToolType { get; set; } = InkToolType.Pen;

    [JsonIgnore]
    public Color Color
    {
        get => Color.FromArgb(ColorA, ColorR, ColorG, ColorB);
        set
        {
            ColorA = value.A;
            ColorR = value.R;
            ColorG = value.G;
            ColorB = value.B;
        }
    }

    public InkStroke() { }

    public InkStroke(Color color, float strokeWidth, InkToolType toolType = InkToolType.Pen)
    {
        Color = color;
        StrokeWidth = strokeWidth;
        ToolType = toolType;
    }
}

