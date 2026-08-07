using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using TerminalEmbellish.templates.@private.BrowserTemplate;
using TerminalEmbellish.templates.@private.Media;

namespace TerminalEmbellish.help;

public static class HelpSystem
{
    private static string? _helpRoot;

    private static string HelpRoot
    {
        get
        {
            if (_helpRoot != null) return _helpRoot;
            var d = AppDomain.CurrentDomain.BaseDirectory;
            while (d != null && !File.Exists(Path.Combine(d, "TerminalEmbellish.Desktop.csproj")))
                d = Path.GetDirectoryName(d);
            if (d == null) d = AppDomain.CurrentDomain.BaseDirectory;
            _helpRoot = Path.Combine(d, "help");
            return _helpRoot;
        }
    }

    public static Button CreateHelpButton(string relativePath = "", double buttonSize = 32)
    {
        var btn = new Button
        {
            Width = buttonSize, Height = buttonSize,
            Background = Brushes.Transparent, BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        var icon = ImageLoader.Place("help", width: buttonSize);
        if (icon != null) btn.Content = icon;
        btn.Click += (_, _) => ShowHelpWindow(relativePath);
        return btn;
    }

    public static void ShowHelpWindow(string relativePath)
    {
        var targetPath = string.IsNullOrEmpty(relativePath) ? HelpRoot : Path.Combine(HelpRoot, relativePath);

        if (File.Exists(targetPath) && targetPath.EndsWith(".html"))
        {
            ShowHelpWindowWithBrowser(targetPath, isHtmlFile: true, Path.GetDirectoryName(targetPath));
        }
        else if (File.Exists(targetPath) && targetPath.EndsWith(".md"))
        {
            ShowHelpWindowWithBrowser(targetPath, isHtmlFile: false, Path.GetDirectoryName(targetPath));
        }
        else if (File.Exists(targetPath + ".html"))
        {
            ShowHelpWindowWithBrowser(targetPath + ".html", isHtmlFile: true, Path.GetDirectoryName(targetPath));
        }
        else if (File.Exists(targetPath + ".md"))
        {
            ShowHelpWindowWithBrowser(targetPath + ".md", isHtmlFile: false, Path.GetDirectoryName(targetPath));
        }
        else
        {
            var window = new Window
            {
                Title = "帮助文案",
                Width = 420, Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowDecorations = WindowDecorations.Full
            };
            ShowList(window, targetPath);
            window.Show();
        }
    }

 private static void ShowHelpWindowWithBrowser(string filePath, bool isHtmlFile, string? parentDir = null, double width = 500, double height = 600)
{
    var window = new Window
    {
        Title = "帮助文案",
        Width = width,
        Height = height,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        WindowDecorations = WindowDecorations.Full
    };

    var rootGrid = new Grid();
    rootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(40)));
    rootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));

    // 顶部返回栏
    var backBtn = new Button
    {
        Content = "← 返回",
        FontSize = 13,
        Foreground = new SolidColorBrush(Color.Parse("#4A9EFF")),
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Padding = new Thickness(0),
        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
    };
    backBtn.Click += (_, _) =>
    {
        window.Close();
        if (parentDir != null)
        {
            var listWindow = new Window
            {
                Title = "帮助文案",
                Width = 420, Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowDecorations = WindowDecorations.Full
            };
            ShowList(listWindow, parentDir);
            listWindow.Show();
        }
    };

    var topBar = new Border
    {
        Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
        Padding = new Thickness(12, 0),
        Child = backBtn
    };
    Grid.SetRow(topBar, 0);

    // 浏览器区域
    var browser = new BrowserControl();
    Grid.SetRow(browser, 1);

    rootGrid.Children.Add(topBar);
    rootGrid.Children.Add(browser);

    window.Content = rootGrid;

    var htmlDir = Path.GetDirectoryName(filePath) ?? "";

    window.Loaded += (_, _) =>
    {
        if (isHtmlFile)
        {
            browser.LoadHtmlFile(filePath);
        }
        else
        {
            var mdContent = File.ReadAllText(filePath);
            var html = MarkdownToHtml(mdContent);
            browser.LoadHtml(html);
        }
    };

    window.Show();
}
   private static string MarkdownToHtml(string md)
{
    var sb = new StringBuilder();
    sb.Append("<html><head><meta charset='utf-8'><style>");
    sb.Append("html,body{height:100%;overflow-y:scroll!important;-webkit-overflow-scrolling:touch;scrollbar-width:none!important;-ms-overflow-style:none!important;}");
    sb.Append("::-webkit-scrollbar{width:0!important;height:0!important;display:none!important;}");
    sb.Append("::-webkit-scrollbar-track{display:none!important;}");
    sb.Append("::-webkit-scrollbar-thumb{display:none!important;}");
    sb.Append("body{font-family:'Microsoft YaHei',SimHei,sans-serif;padding:20px 24px;color:#333;line-height:1.9;background:#fff}");
    sb.Append("h1{font-size:20px;font-weight:bold;color:#000;margin:8px 0 16px 0}");
    sb.Append("li{margin-left:16px;color:#555;font-size:14px}");
    sb.Append("p{font-size:14px;color:#555;margin:4px 0}");
    sb.Append("b,strong{color:#000}");
    sb.Append("</style></head><body>");

    foreach (var line in md.Split('\n'))
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            sb.Append("<br>");
        }
        else if (line.StartsWith("# "))
        {
            sb.Append($"<h1>{EscapeHtml(line.Substring(2))}</h1>");
        }
        else if (line.StartsWith("- "))
        {
            sb.Append($"<li>{ParseBold(EscapeHtml(line.Substring(2)))}</li>");
        }
        else
        {
            sb.Append($"<p>{ParseBold(EscapeHtml(line))}</p>");
        }
    }

    sb.Append("</body></html>");
    return sb.ToString();
}
    private static string EscapeHtml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string ParseBold(string text) =>
        Regex.Replace(text, @"\*\*(.+?)\*\*", "<b>$1</b>");

  private static void ShowList(Window window, string currentPath)
{
    var panel = new StackPanel { Spacing = 8, Margin = new Thickness(16) };

    // 面包屑导航嘎
    var breadRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 2, Margin = new Thickness(0, 0, 0, 8) };
    breadRow.Children.Add(new TextBlock { Text = "📁 根目录", FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#4A9EFF")), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) });
    ((TextBlock)breadRow.Children[0]).PointerPressed += (_, _) => ShowList(window, HelpRoot);

    if (currentPath != HelpRoot)
    {
        var relative = currentPath.Replace(HelpRoot, "").TrimStart(Path.DirectorySeparatorChar);
        var parts = relative.Split(Path.DirectorySeparatorChar);
        var acc = HelpRoot;
        foreach (var part in parts)
        {
            acc = Path.Combine(acc, part);
            var cap = acc;
            breadRow.Children.Add(new TextBlock { Text = " > ", FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#999999")) });
            breadRow.Children.Add(new TextBlock { Text = part, FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#4A9EFF")), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) });
            ((TextBlock)breadRow.Children[breadRow.Children.Count - 1]).PointerPressed += (_, _) => ShowList(window, cap);
        }
    }
    panel.Children.Add(breadRow);
    panel.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.Parse("#E0E0E0")) });

    if (currentPath != HelpRoot)
    {
        var backRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
        var backIcon = ImageLoader.Place("0001", width: 20);
        if (backIcon != null) backRow.Children.Add(backIcon);
        backRow.Children.Add(new TextBlock { Text = "返回上级", FontSize = 14, Foreground = new SolidColorBrush(Color.Parse("#4A9EFF")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
        backRow.PointerPressed += (_, _) => ShowList(window, Path.GetDirectoryName(currentPath) ?? HelpRoot);
        panel.Children.Add(backRow);
    }

    // 子目录 — 红色圆点嘎
    if (Directory.Exists(currentPath))
    {
        foreach (var dir in Directory.GetDirectories(currentPath))
        {
            var dirName = Path.GetFileName(dir);
            var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
            row.Children.Add(new TextBlock { Text = "●", FontSize = 10, Foreground = new SolidColorBrush(Color.Parse("#4A9EFF")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            row.Children.Add(new TextBlock { Text = dirName, FontSize = 14, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#000000")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            var capDir = dir;
            row.PointerPressed += (_, _) => ShowList(window, capDir);
            panel.Children.Add(row);
        }
    }

    // .html 和 .md 文件 — 蓝色圆点嘎
    if (Directory.Exists(currentPath))
    {
        var htmlFiles = Directory.GetFiles(currentPath, "*.html");
        var mdFiles = Directory.GetFiles(currentPath, "*.md");
        var allDocFiles = htmlFiles.Concat(mdFiles).OrderBy(f => f).ToArray();

        foreach (var file in allDocFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var ext = Path.GetExtension(file);
            var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
            row.Children.Add(new TextBlock { Text = "●", FontSize = 10, Foreground = new SolidColorBrush(Color.Parse("#00e1ff")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            row.Children.Add(new TextBlock { Text = fileName, FontSize = 14, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#000000")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            var capFile = file;
            var isHtml = ext == ".html";
            row.PointerPressed += (_, _) => ShowHelpWindowWithBrowser(capFile, isHtml, currentPath);
            panel.Children.Add(row);
        }
    }

    window.Content = new ScrollViewer { Content = panel };
}
}