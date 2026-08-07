using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;

namespace TerminalEmbellish.templates.Components;

public partial class SlidingTab : UserControl
{
    private readonly List<Border> _tabBorders = new();
    private readonly List<Control> _contents = new();
    private int _selectedIndex;
    private double _offsetX;
    private double _tabWidth;
    private double _headerHeight;
    private double _indicatorTop;
    private double _indicatorHeight;

    public SlidingTab()
    {
        InitializeComponent();

        Indicator.Transitions = new Transitions
        {
            new ThicknessTransition { Property = MarginProperty, Duration = TimeSpan.FromMilliseconds(250) }
        };
    }

    public static SlidingTab Create(
        List<(string title, Control content)> tabs,
        double width = 400, double height = 400, double tabWidth = 120, double fontSize = 14,
        string textColor = "#AAAAAA", string indicatorColor = "#667eea",
        double headerHeight = 44, double indicatorTop = 42, double indicatorHeight = 3)
    {
        var tc = new SlidingTab { Width = width, Height = height };
        tc._tabWidth = tabWidth;
        tc._headerHeight = headerHeight;
        tc._indicatorTop = indicatorTop;
        tc._indicatorHeight = indicatorHeight;
        if (tabs.Count < 2) return tc;

        for (int i = 0; i < tabs.Count; i++)
        {
            var index = i;
            var text = new TextBlock
            {
                Text = tabs[i].title, FontSize = fontSize,
                Foreground = new SolidColorBrush(Color.Parse(textColor)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            var border = new Border
            {
                Width = tabWidth, Height = headerHeight,
                Background = Brushes.Transparent, Child = text,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            border.PointerPressed += (_, _) => tc.SelectTab(index);
            tc.HeaderPanel.Children.Add(border);
            tc._tabBorders.Add(border);
            tc._contents.Add(tabs[i].content);
        }

        tc.ContentHost.Child = tabs[0].content;
        tc.Indicator.Height = indicatorHeight;
        tc.Indicator.Width = tabWidth;
        tc.Indicator.Background = new SolidColorBrush(Color.Parse(indicatorColor));
        // Indicator 由 XAML Grid.Row 控制位置嘎

        tc.Loaded += (_, _) =>
        {
            double totalTabsWidth = tabWidth * tabs.Count;
            tc._offsetX = (tc.Root.Bounds.Width - totalTabsWidth) / 2;
            if (tc._offsetX < 0) tc._offsetX = 0;
            tc.Indicator.Margin = new Thickness(tc._offsetX, indicatorTop, 0, 0);
        };
        return tc;
    }

    private void SelectTab(int index)
    {
        if (index == _selectedIndex) return;
        _selectedIndex = index;
        double targetX = _offsetX + index * _tabWidth;
        Indicator.Margin = new Thickness(targetX, _indicatorTop, 0, 0);
        ContentHost.Child = _contents[_selectedIndex];
    }
}


