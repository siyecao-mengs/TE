using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace TerminalEmbellish.templates.@private.Behaviors;

public static class WindowBehavior
{
  
    private static bool _isResizing;

    public static void Enable(Window window)
    {
        window.PointerMoved += OnPointerMoved;
        window.PointerPressed += OnPointerPressed;
        window.PointerReleased += OnPointerReleased;
    }

    public static void EnableDragMove(Window window, Control dragRegion)
    {
        dragRegion.PointerPressed += (_, e) => window.BeginMoveDrag(e);
    }

    private static void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Window w || _isResizing) return;
        var pos = e.GetPosition(w);
        double m = 8;
        bool l = pos.X <= m, r = pos.X >= w.Width - m, t = pos.Y <= m, b = pos.Y >= w.Height - m;

        if ((l || r) && (t || b))
        {
            if (l && t) w.Cursor = new Cursor(StandardCursorType.TopLeftCorner);
            else if (r && t) w.Cursor = new Cursor(StandardCursorType.TopRightCorner);
            else if (l && b) w.Cursor = new Cursor(StandardCursorType.BottomLeftCorner);
            else w.Cursor = new Cursor(StandardCursorType.BottomRightCorner);
        }
        else if (l || r) w.Cursor = new Cursor(StandardCursorType.SizeWestEast);
        else if (t || b) w.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
        else w.Cursor = new Cursor(StandardCursorType.Arrow);
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Window w) return;
        var pos = e.GetPosition(w);
        double m = 8;
        bool l = pos.X <= m, r = pos.X >= w.Width - m, t = pos.Y <= m, b = pos.Y >= w.Height - m;

        if (l || r || t || b)
        {
            _isResizing = true;
            WindowEdge edge;
            if (l && t) edge = WindowEdge.NorthWest;
            else if (r && t) edge = WindowEdge.NorthEast;
            else if (l && b) edge = WindowEdge.SouthWest;
            else if (r && b) edge = WindowEdge.SouthEast;
            else if (l) edge = WindowEdge.West;
            else if (r) edge = WindowEdge.East;
            else if (t) edge = WindowEdge.North;
            else edge = WindowEdge.South;
            w.BeginResizeDrag(edge, e);
        }
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isResizing = false;
    }
}

