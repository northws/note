using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using HandwrittenNotes.Models;
using Windows.UI;

namespace HandwrittenNotes.Controls;

public sealed partial class InkCanvas : UserControl
{
    private List<InkStroke> _strokes = [];
    private InkStroke? _currentStroke;
    private readonly Stack<List<InkStroke>> _undoStack = new();
    private readonly Stack<List<InkStroke>> _redoStack = new();
    private bool _isDrawing;

    private string? _backgroundImagePath;
    private CanvasBitmap? _backgroundImage;
    
    private float _canvasWidth = 1200;
    private float _canvasHeight = 1600;
    private float _zoom = 1.0f;
    private string _backgroundColor = "#FFFFFF";
    private string _backgroundType = "grid";
    
    private bool _isPenActive;
    private uint? _activePenId;
    private readonly HashSet<uint> _activeTouchIds = new();
    private DateTime _penLastActiveTime;
    private readonly TimeSpan _palmRejectionDelay = TimeSpan.FromMilliseconds(500);
    private bool _penInRange;

    public Color StrokeColor { get; set; } = Color.FromArgb(255, 30, 30, 46);
    public float StrokeWidth { get; set; } = 2.5f;
    public InkToolType CurrentTool { get; set; } = InkToolType.Pen;

    public event EventHandler? StrokesChanged;
    public event EventHandler<float>? ZoomChanged;

    public float Zoom => _zoom;

    public InkCanvas()
    {
        this.InitializeComponent();
    }

    public void SetCanvasSize(float width, float height)
    {
        _canvasWidth = width;
        _canvasHeight = height;
        DrawCanvas.Width = width;
        DrawCanvas.Height = height;
        DrawCanvas.Invalidate();
    }

    public void SetBackgroundStyle(string backgroundColor, string backgroundType)
    {
        _backgroundColor = backgroundColor;
        _backgroundType = backgroundType;
        DrawCanvas.Invalidate();
    }

    public void LoadStrokes(List<InkStroke> strokes)
    {
        _strokes = strokes.Select(CloneStroke).ToList();
        _undoStack.Clear();
        _redoStack.Clear();
        DrawCanvas.Invalidate();
    }

    public async Task LoadBackgroundImageAsync(string imagePath)
    {
        try
        {
            _backgroundImagePath = imagePath;

            if (_backgroundImage != null)
            {
                _backgroundImage.Dispose();
                _backgroundImage = null;
            }

            if (File.Exists(imagePath))
            {
                _backgroundImage = await CanvasBitmap.LoadAsync(DrawCanvas, imagePath);
                if (_backgroundImage != null)
                {
                    _canvasWidth = _backgroundImage.SizeInPixels.Width;
                    _canvasHeight = _backgroundImage.SizeInPixels.Height;
                    DrawCanvas.Width = _canvasWidth;
                    DrawCanvas.Height = _canvasHeight;
                }
            }

            DrawCanvas.Invalidate();
        }
        catch
        {
            _backgroundImage = null;
            DrawCanvas.Invalidate();
        }
    }

    public void ClearBackground()
    {
        _backgroundImage?.Dispose();
        _backgroundImage = null;
        _backgroundImagePath = null;
        DrawCanvas.Width = _canvasWidth;
        DrawCanvas.Height = _canvasHeight;
        DrawCanvas.Invalidate();
    }

    public List<InkStroke> GetStrokes() => _strokes.Select(CloneStroke).ToList();

    public void Clear()
    {
        SaveUndoState();
        _strokes.Clear();
        DrawCanvas.Invalidate();
        StrokesChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool Undo()
    {
        if (_undoStack.Count == 0) return false;
        _redoStack.Push(_strokes.Select(CloneStroke).ToList());
        _strokes = _undoStack.Pop();
        DrawCanvas.Invalidate();
        StrokesChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool Redo()
    {
        if (_redoStack.Count == 0) return false;
        _undoStack.Push(_strokes.Select(CloneStroke).ToList());
        _strokes = _redoStack.Pop();
        DrawCanvas.Invalidate();
        StrokesChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void ZoomIn()
    {
        _zoom = Math.Min(_zoom * 1.25f, 4.0f);
        ApplyZoom();
    }

    public void ZoomOut()
    {
        _zoom = Math.Max(_zoom / 1.25f, 0.25f);
        ApplyZoom();
    }

    public void ResetZoom()
    {
        _zoom = 1.0f;
        ApplyZoom();
    }

    private void ApplyZoom()
    {
        CanvasScrollViewer.ZoomToFactor(_zoom);
        ZoomChanged?.Invoke(this, _zoom);
    }

    private void CanvasScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(CanvasScrollViewer).Properties.MouseWheelDelta;
        if (e.KeyModifiers == Windows.System.VirtualKeyModifiers.Control)
        {
            if (delta > 0)
            {
                ZoomIn();
            }
            else
            {
                ZoomOut();
            }
            e.Handled = true;
        }
    }

    private void DrawCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
    }

    private void DrawCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var ds = args.DrawingSession;
        
        var bgColor = ParseHexColor(_backgroundColor);
        ds.Clear(bgColor);

        if (_backgroundImage != null)
        {
            ds.DrawImage(_backgroundImage, 0, 0);
        }
        else
        {
            DrawBackground(ds, new Windows.Foundation.Size(_canvasWidth, _canvasHeight), _backgroundType);
        }

        foreach (var stroke in _strokes)
        {
            DrawStroke(ds, stroke);
        }

        if (_currentStroke != null)
        {
            DrawStroke(ds, _currentStroke);
        }
    }

    private void DrawBackground(CanvasDrawingSession ds, Windows.Foundation.Size size, string type)
    {
        var lineColor = Windows.UI.Color.FromArgb(40, 128, 128, 128);
        
        switch (type)
        {
            case "grid":
                DrawGrid(ds, size, lineColor, 30f);
                break;
            case "lined":
                DrawLines(ds, size, lineColor, 30f);
                break;
            case "dotted":
                DrawDots(ds, size, lineColor, 30f);
                break;
            case "blank":
            default:
                break;
        }
    }

    private void DrawGrid(CanvasDrawingSession ds, Windows.Foundation.Size size, Windows.UI.Color color, float spacing)
    {
        for (float x = 0; x < size.Width; x += spacing)
        {
            ds.DrawLine(x, 0, x, (float)size.Height, color, 0.5f);
        }
        for (float y = 0; y < size.Height; y += spacing)
        {
            ds.DrawLine(0, y, (float)size.Width, y, color, 0.5f);
        }
    }

    private void DrawLines(CanvasDrawingSession ds, Windows.Foundation.Size size, Windows.UI.Color color, float spacing)
    {
        for (float y = spacing; y < size.Height; y += spacing)
        {
            ds.DrawLine(0, y, (float)size.Width, y, color, 0.5f);
        }
    }

    private void DrawDots(CanvasDrawingSession ds, Windows.Foundation.Size size, Windows.UI.Color color, float spacing)
    {
        for (float x = spacing; x < size.Width; x += spacing)
        {
            for (float y = spacing; y < size.Height; y += spacing)
            {
                ds.FillCircle(x, y, 1.5f, color);
            }
        }
    }

    private static Windows.UI.Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        byte r = Convert.ToByte(hex[..2], 16);
        byte g = Convert.ToByte(hex[2..4], 16);
        byte b = Convert.ToByte(hex[4..6], 16);
        return Windows.UI.Color.FromArgb(255, r, g, b);
    }

    private static void DrawStroke(CanvasDrawingSession ds, InkStroke stroke)
    {
        if (stroke.Points.Count < 2) return;

        var color = stroke.Color;
        if (stroke.ToolType == InkToolType.Highlighter)
        {
            color = Color.FromArgb(100, color.R, color.G, color.B);
        }

        for (int i = 1; i < stroke.Points.Count; i++)
        {
            var p0 = stroke.Points[i - 1];
            var p1 = stroke.Points[i];

            float avgPressure = (p0.Pressure + p1.Pressure) / 2f;
            float width = stroke.StrokeWidth * (0.3f + avgPressure * 0.7f);

            if (stroke.ToolType == InkToolType.Highlighter)
            {
                width *= 4f;
            }

            ds.DrawLine(
                new Vector2(p0.X, p0.Y),
                new Vector2(p1.X, p1.Y),
                color,
                width,
                new CanvasStrokeStyle
                {
                    StartCap = CanvasCapStyle.Round,
                    EndCap = CanvasCapStyle.Round,
                    LineJoin = CanvasLineJoin.Round
                });
        }
    }

    private void DrawCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pointerDevice = e.Pointer.PointerDeviceType;
        var pointerId = e.Pointer.PointerId;

        if (pointerDevice == PointerDeviceType.Touch)
        {
            _activeTouchIds.Add(pointerId);
            
            if (_isPenActive || _penInRange || IsInPalmRejectionWindow())
            {
                e.Handled = true;
                return;
            }
        }

        if (pointerDevice == PointerDeviceType.Pen)
        {
            _isPenActive = true;
            _activePenId = pointerId;
            _penInRange = true;
            _penLastActiveTime = DateTime.Now;
            DisableScrolling();
        }

        if (pointerDevice == PointerDeviceType.Mouse && _isPenActive)
        {
            e.Handled = true;
            return;
        }

        if (CurrentTool == InkToolType.Eraser)
        {
            HandleEraserPress(e);
            return;
        }

        var point = e.GetCurrentPoint(DrawCanvas);
        _isDrawing = true;
        DrawCanvas.CapturePointer(e.Pointer);

        SaveUndoState();

        _currentStroke = new InkStroke(StrokeColor, StrokeWidth, CurrentTool);
        _currentStroke.Points.Add(new InkPoint(
            (float)point.Position.X,
            (float)point.Position.Y,
            point.Properties.Pressure));

        e.Handled = true;
    }

    private void DrawCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var pointerDevice = e.Pointer.PointerDeviceType;
        var pointerId = e.Pointer.PointerId;

        if (pointerDevice == PointerDeviceType.Touch)
        {
            if (_isPenActive || _penInRange || IsInPalmRejectionWindow())
            {
                e.Handled = true;
                return;
            }
        }

        if (pointerDevice == PointerDeviceType.Pen)
        {
            _penLastActiveTime = DateTime.Now;
        }

        if (pointerDevice == PointerDeviceType.Mouse && _isPenActive)
        {
            e.Handled = true;
            return;
        }

        if (CurrentTool == InkToolType.Eraser && _isDrawing)
        {
            HandleEraserMove(e);
            return;
        }

        if (!_isDrawing || _currentStroke == null) return;

        var point = e.GetCurrentPoint(DrawCanvas);
        _currentStroke.Points.Add(new InkPoint(
            (float)point.Position.X,
            (float)point.Position.Y,
            point.Properties.Pressure));

        DrawCanvas.Invalidate();
        e.Handled = true;
    }

    private void DrawCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var pointerDevice = e.Pointer.PointerDeviceType;
        var pointerId = e.Pointer.PointerId;

        if (pointerDevice == PointerDeviceType.Touch)
        {
            _activeTouchIds.Remove(pointerId);
        }

        if (pointerDevice == PointerDeviceType.Pen && _activePenId == pointerId)
        {
            _isPenActive = false;
            _activePenId = null;
            _penLastActiveTime = DateTime.Now;
            EnableScrolling();
        }

        if (!_isDrawing) return;

        if (_currentStroke != null && _currentStroke.Points.Count >= 2)
        {
            _strokes.Add(_currentStroke);
            StrokesChanged?.Invoke(this, EventArgs.Empty);
        }

        _currentStroke = null;
        _isDrawing = false;
        DrawCanvas.ReleasePointerCapture(e.Pointer);
        DrawCanvas.Invalidate();
        e.Handled = true;
    }

    private void DrawCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var pointerDevice = e.Pointer.PointerDeviceType;
        var pointerId = e.Pointer.PointerId;

        if (pointerDevice == PointerDeviceType.Touch)
        {
            _activeTouchIds.Remove(pointerId);
        }

        if (pointerDevice == PointerDeviceType.Pen && _activePenId == pointerId)
        {
            _isPenActive = false;
            _activePenId = null;
            _penInRange = false;
            _penLastActiveTime = DateTime.Now;
            EnableScrolling();
        }

        if (_isDrawing && _currentStroke != null)
        {
            if (_currentStroke.Points.Count >= 2)
            {
                _strokes.Add(_currentStroke);
                StrokesChanged?.Invoke(this, EventArgs.Empty);
            }
            _currentStroke = null;
            _isDrawing = false;
            DrawCanvas.Invalidate();
        }
    }

    private void DrawCanvas_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var pointerDevice = e.Pointer.PointerDeviceType;
        if (pointerDevice == PointerDeviceType.Pen)
        {
            _penInRange = true;
            _activePenId = e.Pointer.PointerId;
            _penLastActiveTime = DateTime.Now;
            DisableScrolling();
        }
    }

    private void DrawCanvas_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        var pointerDevice = e.Pointer.PointerDeviceType;
        var pointerId = e.Pointer.PointerId;

        if (pointerDevice == PointerDeviceType.Touch)
        {
            _activeTouchIds.Remove(pointerId);
        }

        if (pointerDevice == PointerDeviceType.Pen && _activePenId == pointerId)
        {
            _isPenActive = false;
            _activePenId = null;
            _penInRange = false;
            _penLastActiveTime = DateTime.Now;
            EnableScrolling();
        }
    }

    private bool IsInPalmRejectionWindow()
    {
        return DateTime.Now - _penLastActiveTime < _palmRejectionDelay;
    }

    private void DisableScrolling()
    {
        CanvasScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
        CanvasScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
    }

    private void EnableScrolling()
    {
        CanvasScrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
        CanvasScrollViewer.VerticalScrollMode = ScrollMode.Enabled;
    }

    #region 橡皮擦

    private void HandleEraserPress(PointerRoutedEventArgs e)
    {
        _isDrawing = true;
        DrawCanvas.CapturePointer(e.Pointer);
        SaveUndoState();
        EraseAt(e.GetCurrentPoint(DrawCanvas).Position);
        e.Handled = true;
    }

    private void HandleEraserMove(PointerRoutedEventArgs e)
    {
        EraseAt(e.GetCurrentPoint(DrawCanvas).Position);
        e.Handled = true;
    }

    private void EraseAt(Windows.Foundation.Point position)
    {
        float eraserRadius = 15f;
        bool erased = false;

        for (int i = _strokes.Count - 1; i >= 0; i--)
        {
            foreach (var point in _strokes[i].Points)
            {
                float dx = point.X - (float)position.X;
                float dy = point.Y - (float)position.Y;
                if (dx * dx + dy * dy < eraserRadius * eraserRadius)
                {
                    _strokes.RemoveAt(i);
                    erased = true;
                    break;
                }
            }
        }

        if (erased)
        {
            DrawCanvas.Invalidate();
            StrokesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion

    #region 辅助方法

    private void SaveUndoState()
    {
        _undoStack.Push(_strokes.Select(CloneStroke).ToList());
        _redoStack.Clear();
        while (_undoStack.Count > 50)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 0; i < items.Length - 1; i++)
            {
                _undoStack.Push(items[items.Length - 1 - i]);
            }
            break;
        }
    }

    private static InkStroke CloneStroke(InkStroke s)
    {
        var clone = new InkStroke
        {
            ColorA = s.ColorA,
            ColorR = s.ColorR,
            ColorG = s.ColorG,
            ColorB = s.ColorB,
            StrokeWidth = s.StrokeWidth,
            ToolType = s.ToolType,
            Points = s.Points.Select(p => new InkPoint(p.X, p.Y, p.Pressure)).ToList()
        };
        return clone;
    }

    #endregion
}