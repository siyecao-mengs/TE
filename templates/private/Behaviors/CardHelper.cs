using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TerminalEmbellish.templates.@private.Behaviors;

/// <summary>
/// 卡片操作助手嘎：替换/添加卡片
/// </summary>
public static class CardHelper
{
    public static void ReplaceFirstCard(Panel cardPanel, string text)
    {
        if (cardPanel.Children.Count > 0)
            cardPanel.Children.RemoveAt(0);

        cardPanel.Children.Insert(0, new Border
        {
            Width = 400, Height = 200,
            Background = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Colors.DimGray),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(20),
            Margin = new Thickness(3),
            Child = new TextBlock { Text = text, FontSize = 40, Foreground = new SolidColorBrush(Colors.White), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center }
        });
    }
}
