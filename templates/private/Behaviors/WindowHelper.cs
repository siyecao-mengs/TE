using Avalonia.Controls;

namespace TerminalEmbellish.templates.@private.Behaviors;

/// <summary>
/// 万能窗口调用嘎：不依赖 ModalDialog，直接弹独立窗口
/// </summary>
public static class WindowHelper
{
    /// <summary>
    /// 弹出独立窗口嘎
    /// </summary>
    public static void ShowDialog(Control content, double width, double height, string title = "")
    {
        var win = new Window
        {
            Title = title,
            Width = width,
            Height = height,
            Content = content,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowDecorations = WindowDecorations.Full
        };
        win.Show();
    }
}
