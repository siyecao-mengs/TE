using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace TerminalEmbellish.json_ui.slider;

public partial class SliderSquare : UserControl
{
    private double _trackHeight;
    private double _scrollMax;
    private bool _dragging;
    private double _dragStartY;
    private double _dragStartTop;

    public event Action<double>? ScrollChanged;

    public SliderSquare()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
    }

    public void Configure(double size, double trackHeight, double scrollMax, double speed, bool reversed, string color, double opacity)
    {
        _trackHeight = trackHeight;
        _scrollMax = scrollMax;
        Width = size;
        Height = size;
        Thumb.Background = new SolidColorBrush(Color.Parse(color));
        Opacity = opacity;
    }

    public void UpdatePosition(double scrollOffset)
    {
        if (_scrollMax <= 0) return;
        double ratio = scrollOffset / _scrollMax;
        double maxTop = _trackHeight - Height;
        double top = ratio * maxTop;
        Margin = new Thickness(0, top, 0, 0);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragging = true;
        var point = e.GetPosition(this);
        _dragStartY = point.Y;
        _dragStartTop = Margin.Top;
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        var point = e.GetPosition(this);
        double delta = point.Y - _dragStartY;
        double newTop = _dragStartTop + delta;
        double maxTop = _trackHeight - Height;
        newTop = Math.Clamp(newTop, 0, maxTop);
        Margin = new Thickness(0, newTop, 0, 0);

        double ratio = newTop / maxTop;
        double scrollTarget = ratio * _scrollMax;
        ScrollChanged?.Invoke(scrollTarget);
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
    }
}
