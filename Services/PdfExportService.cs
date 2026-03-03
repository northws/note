using HandwrittenNotes.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace HandwrittenNotes.Services;

public class PdfExportService
{
    public async Task<bool> ExportToPdfAsync(Notebook notebook)
    {
        try
        {
            var file = await PickSaveFileAsync();
            if (file == null) return false;

            using var document = new PdfDocument();
            document.Info.Title = notebook.Name;
            document.Info.Author = "Handwritten Notes";

            foreach (var page in notebook.Pages)
            {
                var pageWidth = page.Width > 0 ? page.Width : 794;
                var pageHeight = page.Height > 0 ? page.Height : 1123;

                var pdfPage = document.AddPage();
                pdfPage.Width = new XUnit(pageWidth);
                pdfPage.Height = new XUnit(pageHeight);

                using var gfx = XGraphics.FromPdfPage(pdfPage);

                gfx.DrawRectangle(XBrushes.White, 0, 0, pageWidth, pageHeight);

                if (!string.IsNullOrEmpty(page.BackgroundImagePath) && File.Exists(page.BackgroundImagePath))
                {
                    try
                    {
                        using var image = XImage.FromFile(page.BackgroundImagePath);
                        gfx.DrawImage(image, 0, 0, pageWidth, pageHeight);
                    }
                    catch
                    {
                    }
                }

                foreach (var stroke in page.Strokes)
                {
                    DrawStroke(gfx, stroke);
                }
            }

            document.Save(file.Path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void DrawStroke(XGraphics gfx, InkStroke stroke)
    {
        if (stroke.Points.Count < 2) return;

        var color = XColor.FromArgb(stroke.ColorA, stroke.ColorR, stroke.ColorG, stroke.ColorB);
        if (stroke.ToolType == InkToolType.Highlighter)
        {
            color = XColor.FromArgb(100, stroke.ColorR, stroke.ColorG, stroke.ColorB);
        }

        for (int i = 1; i < stroke.Points.Count; i++)
        {
            var p0 = stroke.Points[i - 1];
            var p1 = stroke.Points[i];

            float avgPressure = (p0.Pressure + p1.Pressure) / 2f;
            double width = stroke.StrokeWidth * (0.3 + avgPressure * 0.7);

            if (stroke.ToolType == InkToolType.Highlighter)
            {
                width *= 4;
            }

            var pen = new XPen(color, width)
            {
                LineCap = XLineCap.Round,
                LineJoin = XLineJoin.Round
            };

            gfx.DrawLine(pen, p0.X, p0.Y, p1.X, p1.Y);
        }
    }

    private static async Task<StorageFile?> PickSaveFileAsync()
    {
        var savePicker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"笔记_{DateTime.Now:yyyy-MM-dd}"
        };

        savePicker.FileTypeChoices.Add("PDF文档", [".pdf"]);

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance!);
        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

        return await savePicker.PickSaveFileAsync();
    }
}