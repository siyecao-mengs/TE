using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TerminalEmbellish.templates.Components;

public partial class VerticalList : UserControl
{
    public VerticalList()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 创建竖形列表嘎
    /// </summary>
    /// <param name="items">每一项的内容控件嘎</param>
    /// <param name="sectionTitle">段落标题嘎（null=不显示）</param>
    /// <param name="titleAlignment">标题对齐：Left/Center/Right 嘎</param>
    /// <param name="titleColor">标题颜色嘎</param>
    /// <param name="titleMargin">标题边距嘎</param>
    /// <param name="itemHeight">每个长方形条高度嘎</param>
    /// <param name="spacing">间距嘎</param>
    /// <param name="top">距顶部距离嘎</param>
    /// <param name="left">距左边距离嘎</param>
    /// <param name="right">距右边距离嘎</param>
    /// <param name="cornerRadius">圆角嘎</param>
    /// <param name="backgroundColor">背景色嘎（前两位是Alpha透明度）</param>
    /// <param name="borderColor">边框颜色嘎</param>
    /// <param name="borderThickness">边框粗细嘎</param>
    public static VerticalList Create(
        List<Control> items,
        string? sectionTitle = null,
        string titleAlignment = "Left",
        string titleColor = "#888888",
        Thickness? titleMargin = null,
        double itemHeight = 50,
        double spacing = 8,
        double top = 0,
        double left = 0,
        double right = 0,
        double cornerRadius = 22,
        string backgroundColor = "#66FFFFFF",
        string borderColor = "#CCCCCC",
        double borderThickness = 0)
    {
        var list = new VerticalList
        {
            Margin = new Thickness(left, top, right, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };

        list.ListPanel.Spacing = spacing;

        // 段落标题嘎
        if (!string.IsNullOrEmpty(sectionTitle))
        {
            var titleBlock = new TextBlock
            {
                Text = sectionTitle,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse(titleColor)),
                Margin = titleMargin ?? new Thickness(4, 0, 0, 4)
            };

            titleBlock.HorizontalAlignment = titleAlignment switch
            {
                "Center" => Avalonia.Layout.HorizontalAlignment.Center,
                "Right" => Avalonia.Layout.HorizontalAlignment.Right,
                _ => Avalonia.Layout.HorizontalAlignment.Left
            };

            list.ListPanel.Children.Add(titleBlock);
        }

        foreach (var item in items)
        {
            var container = new Border
            {
                Height = itemHeight,
                CornerRadius = new CornerRadius(cornerRadius),
                Background = new SolidColorBrush(Color.Parse(backgroundColor)),
                BorderBrush = new SolidColorBrush(Color.Parse(borderColor)),
                BorderThickness = new Thickness(borderThickness),
                Padding = new Thickness(12, 0),
                Child = item
            };
            list.ListPanel.Children.Add(container);
        }

        return list;
    }
}