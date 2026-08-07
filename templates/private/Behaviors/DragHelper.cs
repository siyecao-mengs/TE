using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace TerminalEmbellish.templates.@private.Behaviors;

public static class DragHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOMOVE = 0x0002;

    // ====== 拖拽移动 ======
    private static bool _moving;
    private static int _clickScreenX, _clickScreenY, _winStartX, _winStartY;

    public static void EnableDrag(Control dragControl)
    {
        dragControl.Cursor = new Cursor(StandardCursorType.Hand);
        dragControl.PointerPressed += (_, e) =>
        {
            var hwnd = GetActiveWindow();
            GetWindowRect(hwnd, out var rect);
            _winStartX = rect.Left; _winStartY = rect.Top;
            GetCursorPos(out var cursor);
            _clickScreenX = cursor.X; _clickScreenY = cursor.Y;
            _moving = true;
        };
        dragControl.PointerMoved += (_, e) =>
        {
            if (!_moving) return;
            GetCursorPos(out var cursor);
            SetWindowPos(GetActiveWindow(), IntPtr.Zero,
                _winStartX + (cursor.X - _clickScreenX),
                _winStartY + (cursor.Y - _clickScreenY),
                0, 0, SWP_NOSIZE | SWP_NOZORDER);
        };
        dragControl.PointerReleased += (_, _) => _moving = false;
    }

    // ====== 边缘缩放 ======
    private static bool _resizing;
    private static int _rsWinX, _rsWinY, _rsWinW, _rsWinH, _rsCursorX, _rsCursorY;
    private static string _rsEdge = "";

    public static void EnableResize(Window window)
    {
        window.PointerMoved += (_, e) =>
        {
            if (_resizing) return;
            var pos = e.GetPosition(window);
            double m = 6;
            bool l = pos.X <= m, r = pos.X >= window.Width - m;
            bool t = pos.Y <= m, b = pos.Y >= window.Height - m;

            if (l && t) window.Cursor = new Cursor(StandardCursorType.TopLeftCorner);
            else if (r && t) window.Cursor = new Cursor(StandardCursorType.TopRightCorner);
            else if (l && b) window.Cursor = new Cursor(StandardCursorType.BottomLeftCorner);
            else if (r && b) window.Cursor = new Cursor(StandardCursorType.BottomRightCorner);
            else if (l || r) window.Cursor = new Cursor(StandardCursorType.SizeWestEast);
            else if (t || b) window.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
            else window.Cursor = new Cursor(StandardCursorType.Arrow);
        };

        window.PointerPressed += (_, e) =>
        {
            var pos = e.GetPosition(window);
            double m = 6;
            bool l = pos.X <= m, r = pos.X >= window.Width - m;
            bool t = pos.Y <= m, b = pos.Y >= window.Height - m;
            if (!l && !r && !t && !b) return;

            var hwnd = GetActiveWindow();
            GetWindowRect(hwnd, out var rect);
            _rsWinX = rect.Left; _rsWinY = rect.Top;
            _rsWinW = rect.Right - rect.Left; _rsWinH = rect.Bottom - rect.Top;
            GetCursorPos(out var cursor);
            _rsCursorX = cursor.X; _rsCursorY = cursor.Y;

            _rsEdge = $"{(l ? "L" : r ? "R" : "")}{(t ? "T" : b ? "B" : "")}";
            _resizing = true;
        };

        window.PointerMoved += (_, e) =>
        {
            if (!_resizing) return;
            GetCursorPos(out var cursor);
            int dx = cursor.X - _rsCursorX, dy = cursor.Y - _rsCursorY;
            int newX = _rsWinX, newY = _rsWinY, newW = _rsWinW, newH = _rsWinH;

            if (_rsEdge.Contains("L")) { newX = _rsWinX + dx; newW = _rsWinW - dx; }
            if (_rsEdge.Contains("R")) { newW = _rsWinW + dx; }
            if (_rsEdge.Contains("T")) { newY = _rsWinY + dy; newH = _rsWinH - dy; }
            if (_rsEdge.Contains("B")) { newH = _rsWinH + dy; }

            if (newW < 460) newW = 460;
            if (newH < 880) newH = 880;

            SetWindowPos(GetActiveWindow(), IntPtr.Zero, newX, newY, newW, newH, SWP_NOZORDER);
        };

        window.PointerReleased += (_, _) => _resizing = false;
    }
}

