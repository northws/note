using HandwrittenNotes.Models;

namespace HandwrittenNotes.Services;

public static class NotebookTemplateService
{
    public static List<InkStroke> BuildTemplateStrokes(string templateType, double pageWidth, double pageHeight)
    {
        var width = (float)Math.Max(1, pageWidth);
        var height = (float)Math.Max(1, pageHeight);

        return templateType switch
        {
            "cornell" => BuildCornellTemplate(width, height),
            "meeting" => BuildMeetingTemplate(width, height),
            "quadrant" => BuildQuadrantTemplate(width, height),
            _ => new List<InkStroke>()
        };
    }

    private static List<InkStroke> BuildCornellTemplate(float width, float height)
    {
        var strokes = new List<InkStroke>();
        var margin = width * 0.05f;
        var headerBottom = MathF.Min(height * 0.14f, 180f);
        var cueColumnX = width * 0.30f;
        var summaryTop = height * 0.80f;

        strokes.Add(CreateLine(margin, headerBottom, width - margin, headerBottom, 1.8f));
        strokes.Add(CreateLine(cueColumnX, headerBottom, cueColumnX, summaryTop, 1.8f));
        strokes.Add(CreateLine(margin, summaryTop, width - margin, summaryTop, 1.8f));
        strokes.Add(CreateLine(margin, margin, width - margin, margin, 1.2f));
        strokes.Add(CreateLine(margin, margin, margin, height - margin, 1.2f));
        strokes.Add(CreateLine(width - margin, margin, width - margin, height - margin, 1.2f));
        strokes.Add(CreateLine(margin, height - margin, width - margin, height - margin, 1.2f));
        return strokes;
    }

    private static List<InkStroke> BuildMeetingTemplate(float width, float height)
    {
        var strokes = new List<InkStroke>();
        var margin = width * 0.05f;
        var headerBottom = MathF.Min(height * 0.16f, 200f);
        var timelineColumnX = width * 0.20f;
        var actionColumnX = width * 0.72f;

        strokes.Add(CreateLine(margin, headerBottom, width - margin, headerBottom, 1.8f));
        strokes.Add(CreateLine(timelineColumnX, headerBottom, timelineColumnX, height - margin, 1.6f));
        strokes.Add(CreateLine(actionColumnX, headerBottom, actionColumnX, height - margin, 1.6f));

        var rowHeight = (height - headerBottom - margin) / 12f;
        for (var row = 1; row <= 12; row++)
        {
            var y = headerBottom + row * rowHeight;
            strokes.Add(CreateLine(margin, y, width - margin, y, 0.9f));
        }

        strokes.Add(CreateLine(margin, margin, width - margin, margin, 1.2f));
        strokes.Add(CreateLine(margin, margin, margin, height - margin, 1.2f));
        strokes.Add(CreateLine(width - margin, margin, width - margin, height - margin, 1.2f));
        strokes.Add(CreateLine(margin, height - margin, width - margin, height - margin, 1.2f));
        return strokes;
    }

    private static List<InkStroke> BuildQuadrantTemplate(float width, float height)
    {
        var strokes = new List<InkStroke>();
        var margin = width * 0.05f;
        var centerX = width / 2f;
        var centerY = height / 2f;
        var headerBottom = MathF.Min(height * 0.12f, 160f);

        strokes.Add(CreateLine(margin, headerBottom, width - margin, headerBottom, 1.8f));
        strokes.Add(CreateLine(centerX, headerBottom, centerX, height - margin, 2.2f));
        strokes.Add(CreateLine(margin, centerY, width - margin, centerY, 2.2f));
        strokes.Add(CreateLine(margin, margin, width - margin, margin, 1.2f));
        strokes.Add(CreateLine(margin, margin, margin, height - margin, 1.2f));
        strokes.Add(CreateLine(width - margin, margin, width - margin, height - margin, 1.2f));
        strokes.Add(CreateLine(margin, height - margin, width - margin, height - margin, 1.2f));
        return strokes;
    }

    private static InkStroke CreateLine(float x1, float y1, float x2, float y2, float strokeWidth)
    {
        return new InkStroke
        {
            ToolType = InkToolType.Pen,
            StrokeWidth = strokeWidth,
            ColorA = 255,
            ColorR = 168,
            ColorG = 176,
            ColorB = 190,
            Points = new List<InkPoint>
            {
                new InkPoint(x1, y1, 0.5f),
                new InkPoint(x2, y2, 0.5f)
            }
        };
    }
}


