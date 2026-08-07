#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TerminalEmbellish.templates.@private.Behaviors;
using TerminalEmbellish.templates.@private.Media;
using TerminalEmbellish.templates.Components;
using TerminalEmbellish.help;

namespace TerminalEmbellish.page_ui.dialogs;

public partial class Settings : UserControl
{
    private static string WallpaperConfigPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".TerminalEmbellish", "wallpaper.json");

    private StackPanel? _changelogStackPanel;

    public Settings()
    {
        InitializeComponent();

        BtnMin.Click += (_, _) =>
        {
            var tl = TopLevel.GetTopLevel(this);
            if (tl is Window win) win.WindowState = WindowState.Minimized;
        };

        BtnMax.Click += (_, _) =>
        {
            var tl = TopLevel.GetTopLevel(this);
            if (tl is Window win)
            {
                var screen = win.Screens.Primary;
                if (screen == null) return;
                bool isMax = win.Width >= screen.Bounds.Width && win.Height >= screen.Bounds.Height;
                if (isMax)
                {
                    win.Width = 460; win.Height = 880;
                    win.Position = new PixelPoint(screen.Bounds.X + (screen.Bounds.Width - 460) / 2, screen.Bounds.Y + (screen.Bounds.Height - 880) / 2);
                    LoadIconToButton(BtnMax, "0003", 18, 18);
                }
                else
                {
                    win.Position = new PixelPoint(screen.Bounds.X, screen.Bounds.Y);
                    win.Width = screen.Bounds.Width; win.Height = screen.Bounds.Height;
                    LoadIconToButton(BtnMax, "0005", 18, 18);
                }
            }
        };

        BtnClose.Click += (_, _) => Environment.Exit(0);

        Loaded += async (_, _) =>
        {
            DragHelper.EnableDrag(DragArea);

            var bgPath = FindFile("ManiWindow", ".jpg", ".jpeg", ".png");
            if (bgPath != null)
            {
                double brightness = 0.6;
                var bgImage = new Image
                {
                    Source = new Bitmap(bgPath),
                    Stretch = Stretch.UniformToFill,
                    Opacity = 1.0,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
                bgImage.Effect = new BlurEffect { Radius = 0 };
                RootGrid.Children.Insert(0, bgImage);
                var brightnessOverlay = new Border
                {
                    Background = new SolidColorBrush(Colors.Black, 1.0 - brightness),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
                };
                RootGrid.Children.Insert(1, brightnessOverlay);
            }

            var backPath = FindInIconLib("0001", ".png");
            if (backPath != null)
                BtnBack.Content = new Image { Source = new Bitmap(backPath), Width = 20, Height = 20 };

            BtnBack.Click += (_, _) =>
            {
                var tl = TopLevel.GetTopLevel(this);
                if (tl is TerminalEmbellish.MainWindow mainWindow) mainWindow.GoBackToMain();
            };

            LoadIconToButton(BtnMin, "0002", 18, 18);
            LoadIconToButton(BtnMax, "0003", 18, 18);
            LoadIconToButton(BtnClose, "0004", 18, 18);

            var icon = ImageLoader.Place("icon", width: 100, top: 20);
            if (icon != null) RootGrid.Children.Add(icon);

            BuildSettingsList();

            bool useGitHub = true;
            await CheckVersion(useGitHub);

            var changelogItems = await FetchChangelogAsync();
            PopulateChangelog(changelogItems);
        };
    }

    private async Task CheckVersion(bool useGitHub)
    {
        string localVersion = "v1.0.0-beta";
        string apiUrl = useGitHub
            ? "https://api.github.com/repos/siyecao-mengs/TE/releases/latest"
            : "https://gitee.com/api/v5/repos/siyecao-meng/TE/releases/latest";

        TbStatus.Text = " [正在检测]";
        TbStatus.Foreground = new SolidColorBrush(Color.Parse("#999999"));

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("User-Agent", "TerminalEmbellish");

            var json = await client.GetStringAsync(apiUrl);
            using var doc = JsonDocument.Parse(json);
            var remoteVersion = doc.RootElement.GetProperty("tag_name").GetString() ?? "";

            var local = localVersion.TrimStart('v');
            var remote = remoteVersion.TrimStart('v');

            if (local == remote)
            {
                TbStatus.Text = " [已是最新]";
                TbStatus.Foreground = new SolidColorBrush(Color.Parse("#4CAF50"));
            }
            else
            {
                TbStatus.Text = $" [发现新版: {remoteVersion}]";
                TbStatus.Foreground = new SolidColorBrush(Color.Parse("#F44336"));
            }
        }
        catch
        {
            TbStatus.Text = " [检测失败]";
            TbStatus.Foreground = new SolidColorBrush(Color.Parse("#999999"));
        }
    }

    private void BuildSettingsList()
    {
        SettingsPanel.Children.Clear();

        // ── 通用设置 ──
        var wallpaperItem = CreateClickableItem(
            "更换主页面壁纸",
            "点击选择图片或视频作为主页面背景",
            "pngadd",
            async () =>
            {
                var top = TopLevel.GetTopLevel(this) as Window;
                if (top == null) return;

                var files = await top.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "选择壁纸（图片或视频）",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("图片/视频")
                        {
                            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.mp4", "*.avi", "*.webm" }
                        }
                    }
                });

                if (files != null && files.Count > 0)
                {
                    var path = files[0].Path.LocalPath;
                    SaveWallpaperPath(path);
                    ApplyWallpaper(path);
                }
            });
var resetWallpaperItem = CreateClickableItem(
    "还原默认壁纸",
    "恢复为 TE终端盒子 自带的默认背景",
    "0002_white",
    () =>
    {
        DeleteWallpaperConfig();
        var tl = TopLevel.GetTopLevel(this);
        if (tl is TerminalEmbellish.MainWindow mainWindow)
        {
            mainWindow.ResetToDefaultWallpaper();
        }
    });

SettingsPanel.Children.Add(VerticalList.Create(
    items: new List<Control> { wallpaperItem, resetWallpaperItem },
    sectionTitle: "通用设置", titleAlignment: "Left",
    titleColor: "#FFFFFF",
    itemHeight: 56, spacing: 6, top: 0, left: 0, right: 0,
    cornerRadius: 22, backgroundColor: "#99FFFFFF",
    borderColor: "#00FFFFFF", borderThickness: 0));

        // ── 更新日志 ──
        _changelogStackPanel = new StackPanel { Margin = new Thickness(16, 12, 16, 12) };

        var changelogScroll = new ScrollViewer
        {
            Content = _changelogStackPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var changelogBorder = new Border {
            CornerRadius = new CornerRadius(22),
            ClipToBounds = true,
            Child = changelogScroll
        };

        SettingsPanel.Children.Add(VerticalList.Create(
            items: new List<Control> { changelogBorder },
            sectionTitle: "更新日志", titleAlignment: "Left",
            titleColor: "#FFFFFF",
            itemHeight: 300, spacing: 0, top: 8, left: 0, right: 0,
            cornerRadius: 22, backgroundColor: "#99FFFFFF",
            borderColor: "#00FFFFFF", borderThickness: 0));

        // ── 关于我们 ──
        var qqGroupItem = CreateClickableItem(
            "加入QQ交流群",
            "扫码加入官方交流群，反馈问题、交流使用心得",
            "cmd",
            () =>
            {
                HelpSystem.ShowHelpWindow("关于我们/QQ交流群.html");
            });
        /*         var sponsorItem = CreateClickableItem(
             "赞助我们",
             "如果觉得好用，欢迎请开发者喝杯奶茶",
             "terminal",
             () =>
             {
                 HelpSystem.ShowHelpWindow("关于我们/为TE捐增.html");
             }); */
        SettingsPanel.Children.Add(VerticalList.Create(
            items: new List<Control> { qqGroupItem },
            sectionTitle: "关于我们", titleAlignment: "Left",
            titleColor: "#FFFFFF",
            itemHeight: 56, spacing: 10, top: 8, left: 0, right: 0,
            cornerRadius: 22, backgroundColor: "#99FFFFFF",
            borderColor: "#00FFFFFF", borderThickness: 0));

        // ── 关于TE终端盒子 ──
        var aboutTeItem = CreateClickableItem(
            "关于TE终端盒子",
            "了解项目简介、技术栈与致谢",
            "icon",
            () =>
            {
                HelpSystem.ShowHelpWindow("关于我们/关于TerminalEmbellish.html");
            });
        SettingsPanel.Children.Add(VerticalList.Create(
            items: new List<Control> { aboutTeItem },
            sectionTitle: null, titleAlignment: "Left",
            titleColor: "#FFFFFF",
            itemHeight: 56, spacing: 0, top: 8, left: 0, right: 0,
            cornerRadius: 22, backgroundColor: "#99FFFFFF",
            borderColor: "#00FFFFFF", borderThickness: 0));

        // ── MIT开源许可 ──
        var mitItem = CreateClickableItem(
            "MIT开源许可",
            "查看本项目的 MIT 许可证完整文本",
            "powershell",
            () =>
            {
                HelpSystem.ShowHelpWindow("开源许可/MIT许可协议.html");
            });
        SettingsPanel.Children.Add(VerticalList.Create(
            items: new List<Control> { mitItem },
            sectionTitle: null, titleAlignment: "Left",
            titleColor: "#FFFFFF",
            itemHeight: 56, spacing: 0, top: 8, left: 0, right: 0,
            cornerRadius: 22, backgroundColor: "#99FFFFFF",
            borderColor: "#00FFFFFF", borderThickness: 0));

        // ── 第三方开源许可 ──
        var thirdPartyItem = CreateClickableItem(
            "第三方开源许可",
            "查看本项目使用的所有开源库及其许可证",
            "siyecao",
            () =>
            {
                HelpSystem.ShowHelpWindow("开源许可/第三方开源许可");
            });
        SettingsPanel.Children.Add(VerticalList.Create(
            items: new List<Control> { thirdPartyItem },
            sectionTitle: null, titleAlignment: "Left",
            titleColor: "#FFFFFF",
            itemHeight: 56, spacing: 0, top: 8, left: 0, right: 0,
            cornerRadius: 22, backgroundColor: "#99FFFFFF",
            borderColor: "#00FFFFFF", borderThickness: 0));

        // ── 用户隐私与协议 ──
        var privacyItem = CreateClickableItem(
            "用户隐私与协议",
            "查看我们的隐私政策与服务条款",
            "on",
            () =>
            {
                HelpSystem.ShowHelpWindow("关于我们/用户隐私与协议.html");
            });
        SettingsPanel.Children.Add(VerticalList.Create(
            items: new List<Control> { privacyItem },
            sectionTitle: null, titleAlignment: "Left",
            titleColor: "#FFFFFF",
            itemHeight: 56, spacing: 0, top: 8, left: 0, right: 0,
            cornerRadius: 22, backgroundColor: "#99FFFFFF",
            borderColor: "#00FFFFFF", borderThickness: 0));
    }

    private static void SaveWallpaperPath(string path)
    {
        var dir = Path.GetDirectoryName(WallpaperConfigPath);
        if (dir != null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(new { wallpaper = path });
        File.WriteAllText(WallpaperConfigPath, json);
    }

    private static string? LoadWallpaperPath()
    {
        if (!File.Exists(WallpaperConfigPath)) return null;
        try
        {
            var json = File.ReadAllText(WallpaperConfigPath);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("wallpaper").GetString();
        }
        catch { return null; }
    }

    public static string? LoadWallpaperPathStatic()
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".TerminalEmbellish", "wallpaper.json");
        if (!File.Exists(configPath)) return null;
        try
        {
            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("wallpaper").GetString();
        }
        catch { return null; }
    }

    private static void DeleteWallpaperConfig()
    {
        if (File.Exists(WallpaperConfigPath))
            File.Delete(WallpaperConfigPath);
    }

    private void ApplyWallpaper(string path)
    {
        var tl = TopLevel.GetTopLevel(this);
        if (tl is TerminalEmbellish.MainWindow mainWindow)
        {
            mainWindow.ApplyStartupWallpaper(path);
        }
    }

    private Control CreateClickableItem(string title, string subtitle, string iconName, Action onClick)
    {
        var textPanel = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        textPanel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = FontWeight.Medium,
            Foreground = new SolidColorBrush(Color.Parse("#000000"))
        });
        textPanel.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#999999"))
        });

        var border = new Border
        {
            Child = textPanel,
            Padding = new Thickness(16, 0, 16, 0),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        border.PointerPressed += (_, _) => onClick();
        return border;
    }

    private void LoadIconToButton(Button btn, string iconName, double w, double h)
    {
        var path = FindInIconLib(iconName, ".png");
        if (path != null)
            btn.Content = new Image { Source = new Bitmap(path), Width = w, Height = h };
    }

    private static string? FindFile(string fileName, params string[] extensions)
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "TerminalEmbellish.Desktop.csproj")))
            dir = Path.GetDirectoryName(dir);
        if (dir == null) dir = AppDomain.CurrentDomain.BaseDirectory;
        foreach (var ext in extensions) { var p = Path.Combine(dir, fileName + ext); if (File.Exists(p)) return p; }
        return null;
    }

    private static string? FindInIconLib(string fileName, params string[] extensions)
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "TerminalEmbellish.Desktop.csproj")))
            dir = Path.GetDirectoryName(dir);
        if (dir == null) dir = AppDomain.CurrentDomain.BaseDirectory;
        var libDir = Path.Combine(dir, "icon_lib");
        if (!Directory.Exists(libDir)) return null;
        foreach (var ext in extensions) { var p = Path.Combine(libDir, fileName + ext); if (File.Exists(p)) return p; }
        return null;
    }

    private async Task<List<(string version, string date, string body)>> FetchChangelogAsync()
    {
        bool useGitHub = true;
        string apiUrl = useGitHub
            ? "https://api.github.com/repos/siyecao-mengs/TE/releases"
            : "https://gitee.com/api/v5/repos/siyecao-meng/TE/releases";

        var result = new List<(string, string, string)>();

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "TerminalEmbellish");

            var json = await client.GetStringAsync(apiUrl);
            using var doc = JsonDocument.Parse(json);

            int count = 0;
            foreach (var release in doc.RootElement.EnumerateArray())
            {
                if (count >= 10) break;

                var tagName = release.GetProperty("tag_name").GetString() ?? "";
                var publishedAt = release.GetProperty("published_at").GetString() ?? "";
                var body = release.GetProperty("body").GetString() ?? "";

                if (DateTime.TryParse(publishedAt, out var date))
                    publishedAt = date.ToString("yyyy年MM月dd日");

                result.Add((tagName, publishedAt, body));
                count++;
            }
        }
        catch
        {
        }

        return result;
    }

    private void PopulateChangelog(List<(string version, string date, string body)> items)
    {
        if (_changelogStackPanel == null) return;

        _changelogStackPanel.Children.Clear();

        if (items.Count == 0)
        {
            var emptyText = new TextBlock
            {
                Text = "暂无发布版本",
                Foreground = new SolidColorBrush(Color.Parse("#999999")),
                FontSize = 14,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 40, 0, 0)
            };
            _changelogStackPanel.Children.Add(emptyText);
            return;
        }

        foreach (var (version, date, body) in items)
        {
            var versionText = new TextBlock
            {
                Text = version,
                Foreground = new SolidColorBrush(Color.Parse("#4A9EFF")),
                FontSize = 15,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 2)
            };
            _changelogStackPanel.Children.Add(versionText);

            var dateText = new TextBlock
            {
                Text = $"📅 {date}",
                Foreground = new SolidColorBrush(Color.Parse("#999999")),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8)
            };
            _changelogStackPanel.Children.Add(dateText);

            var lines = body.Split('\n');
            bool inCodeBlock = false;
            var codeLines = new List<string>();

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("```"))
                {
                    if (inCodeBlock)
                    {
                        var codeText = new TextBlock
                        {
                            Text = string.Join("\n", codeLines),
                            Foreground = new SolidColorBrush(Color.Parse("#333333")),
                            FontSize = 12,
                            FontFamily = new FontFamily("Consolas, Cascadia Code, monospace"),
                            Margin = new Thickness(8, 4, 0, 8)
                        };
                        _changelogStackPanel.Children.Add(codeText);
                        codeLines.Clear();
                        inCodeBlock = false;
                    }
                    else
                    {
                        inCodeBlock = true;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    codeLines.Add(line);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    _changelogStackPanel.Children.Add(new TextBlock { Height = 4 });
                    continue;
                }

                string text = line;
                bool isBold = false;
                if (text.StartsWith("- ") || text.StartsWith("* "))
                {
                    text = "• " + text.Substring(2);
                }
                else if (text.StartsWith("### "))
                {
                    text = text.Substring(4);
                    isBold = true;
                }
                else if (text.StartsWith("## "))
                {
                    text = text.Substring(3);
                    isBold = true;
                }
                else if (text.StartsWith("# "))
                {
                    text = text.Substring(2);
                    isBold = true;
                }

                var lineText = new TextBlock
                {
                    Text = text,
                    Foreground = new SolidColorBrush(Color.Parse("#333333")),
                    FontSize = 13,
                    FontWeight = isBold ? FontWeight.Bold : FontWeight.Normal,
                    Margin = new Thickness(0, 0, 0, 2),
                    TextWrapping = TextWrapping.Wrap
                };
                _changelogStackPanel.Children.Add(lineText);
            }
        }
    }
}