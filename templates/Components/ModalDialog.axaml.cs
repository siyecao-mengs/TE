using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace TerminalEmbellish.templates.Components;

public partial class ModalDialog : Window
{
    private static ModalDialog? _currentDialog;

    public ModalDialog()
    {
        InitializeComponent();
    }

    public static void Dismiss()
    {
        _currentDialog?.Close();
    }

    public static async Task Open(
        Window owner,
        double width = 400,
        double height = 300,
        string position = "Center",
        double overlayOpacity = 0.4,
        double overlayBlur = 0,
        double bodyOpacity = 1.0,
        double bodyPadding = 16,
        string borderColor = "#555555",
        string backgroundColor = "#1a1a2e",
        double borderThickness = 1.5,
        double cornerRadius = 12,
        Control? content = null,
        string animationType = "FadeIn",
        int animationDurationMs = 200,
        bool fadeOutOnClose = true,
        int fadeOutDurationMs = 150,
        int startX = 0,
        int startY = 0,
        bool followOwner = false,
        string contentPosition = "Center",
        double contentX = 0,
        double contentY = 0,
        double marginTop = 0,
        double marginLeft = 0,
        double marginBottom = 0,
        double marginRight = 0,
        bool closeOnOverlayClick = false)
    {
        if (_currentDialog != null) return;

        var dialog = new ModalDialog();
        _currentDialog = dialog;

        dialog.Width = owner.Bounds.Width;
        dialog.Height = owner.Bounds.Height;
        dialog.WindowDecorations = WindowDecorations.None;
        dialog.Background = Brushes.Transparent;
        dialog.WindowStartupLocation = WindowStartupLocation.Manual;
        dialog.Position = owner.Position;

        void OnOwnerMoved(object? s, EventArgs e)
        {
            dialog.Position = owner.Position;
            dialog.Width = owner.Bounds.Width;
            dialog.Height = owner.Bounds.Height;
        }
        owner.PositionChanged += OnOwnerMoved;
        dialog.Closed += (_, _) =>
        {
            owner.PositionChanged -= OnOwnerMoved;
            _currentDialog = null;
        };

        var overlay = new Border
        {
            Background = new SolidColorBrush(Colors.Black, overlayOpacity),
            Cursor = closeOnOverlayClick ? new Cursor(StandardCursorType.Hand) : null,
            IsHitTestVisible = closeOnOverlayClick  // 只有点遮罩关闭时才拦截点击嘎！
        };
        if (closeOnOverlayClick)
            overlay.PointerPressed += (_, _) => dialog.Close();
        if (overlayBlur > 0)
            overlay.Effect = new BlurEffect { Radius = overlayBlur };

        var body = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse(borderColor)),
            BorderThickness = new Thickness(borderThickness),
            CornerRadius = new CornerRadius(cornerRadius),
            Padding = new Thickness(bodyPadding),
            Child = content ?? new TextBlock { Text = "（空内容）", Foreground = Brushes.White },
            Opacity = 0,
            Background = new SolidColorBrush(Color.Parse(backgroundColor), bodyOpacity),
            Width = width,
            Height = height,
            HorizontalAlignment = contentPosition switch
            {
                "TopLeft" or "BottomLeft" or "Manual" => Avalonia.Layout.HorizontalAlignment.Left,
                "TopRight" or "BottomRight" => Avalonia.Layout.HorizontalAlignment.Right,
                _ => Avalonia.Layout.HorizontalAlignment.Center
            },
            VerticalAlignment = contentPosition switch
            {
                "TopLeft" or "TopRight" or "Manual" => Avalonia.Layout.VerticalAlignment.Top,
                "BottomLeft" or "BottomRight" => Avalonia.Layout.VerticalAlignment.Bottom,
                _ => Avalonia.Layout.VerticalAlignment.Center
            },
            Margin = new Thickness(marginLeft, marginTop, marginRight, marginBottom)
        };

        var root = new Grid();
        root.Children.Add(overlay);
        root.Children.Add(body);
        dialog.Content = root;

        dialog.Show(owner);

        int steps = 10;
        int stepMs = animationDurationMs / steps;
        for (int i = 1; i <= steps; i++)
        {
            await Task.Delay(stepMs);
            body.Opacity = (double)i / steps;
        }
        body.Opacity = 1;
    }
}

