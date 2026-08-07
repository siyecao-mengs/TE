using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TerminalEmbellish.templates.Components;

public partial class ListViewTemplate : UserControl
{
    public ListViewTemplate()
    {
        InitializeComponent();
    }

    public static ListViewTemplate Create(
        List<string> items,
        Action<int, string>? onItemClick = null,
        double width = 250,
        double itemHeight = 44,
        double fontSize = 16,
        string textColor = "#FFFFFF",
        string separatorColor = "#333333",
        string hoverColor = "#222222",
        string backgroundColor = "Transparent",
        string fontWeight = "Normal",
        int fontWeightNumber = 400,
        double itemCornerRadius = 0)  // ⭐ 新增：列表项圆角，默认0（直角）
    {
        var list = new ListViewTemplate();
        var stack = new StackPanel { Width = width, ClipToBounds = true };
        stack.Background = new SolidColorBrush(Color.Parse(backgroundColor));

        FontWeight fw = fontWeight switch
        {
            "Bold" => FontWeight.Bold,
            "Light" => FontWeight.Light,
            "Medium" => FontWeight.Medium,
            "Heavy" => FontWeight.Heavy,
            _ => fontWeightNumber switch
            {
                100 => FontWeight.Thin,
                200 => FontWeight.ExtraLight,
                300 => FontWeight.Light,
                400 => FontWeight.Normal,
                500 => FontWeight.Medium,
                600 => FontWeight.SemiBold,
                700 => FontWeight.Bold,
                800 => FontWeight.ExtraBold,
                900 => FontWeight.Heavy,
                _ => FontWeight.Normal
            }
        };

        for (int i = 0; i < items.Count; i++)
        {
            var index = i;
            var item = items[i];

            // 行容器：带圆角嘎！
            var row = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(16, 5, 16, 5),
                CornerRadius = new CornerRadius(itemCornerRadius),  // ⭐ 应用圆角！
                Child = new TextBlock
                {
                    Text = item,
                    FontSize = fontSize,
                    FontWeight = fw,
                    Foreground = new SolidColorBrush(Color.Parse(textColor)),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };

            row.PointerEntered += (_, _) => { row.Background = new SolidColorBrush(Color.Parse(hoverColor)); row.CornerRadius = new CornerRadius(20); };
            row.PointerExited += (_, _) => { row.Background = Brushes.Transparent; row.CornerRadius = new CornerRadius(0); };

            if (onItemClick != null)
            {
                row.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                row.PointerPressed += (_, _) => onItemClick(index, item);
            }

            stack.Children.Add(row);
            
            // 分割线（最后一个不加嘎）
            if (i < items.Count - 1)
            {
                stack.Children.Add(new Border
                {
                    Height = 0.5,
                    Background = new SolidColorBrush(Color.Parse(separatorColor)),
                    Margin = new Thickness(16, 0)
                });
            }
        }

        list.Content = stack;
        return list;
    }
}

