#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using TerminalEmbellish.templates.@private.Behaviors;
using TerminalEmbellish.templates.@private.Media;
using TerminalEmbellish.templates.Components;

using TerminalEmbellish.help;
using TerminalEmbellish.json_ui.slider;
using TerminalEmbellish.page_ui.dialogs;

namespace TerminalEmbellish;

public partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    private const int CardWidth = 400;
    private const int CardHeight = 200;
    private const int CardGap = 6;
    private const int SidePadding = 10;
    private const int CardCount = 1;
    private const double BorderThick = 1.5;
    private const double CornerRadiusVal = 20;
    private static readonly Color BorderColor = Colors.DimGray;
    private static readonly Color BgColor = Color.FromArgb(80, 255, 255, 255);

    private static readonly string AgreementPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".TerminalEmbellish", "agreed.json");

    private UserControl? _slider;
    private MediaManager? _media;
    private Control? _mainContent;

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        DragHelper.EnableResize(this);
        DragHelper.EnableDrag(DragRegion);

        GenerateCards(CardCount);
        LoadCustomSlider();
        SizeChanged += OnWindowSizeChanged;

        BtnDots.Click += async (_, _) =>
        {
            await ModalDialog.Open(
                owner: this,
                width: 130,
                height: 87,
                backgroundColor: "#ffffff",
                borderColor: "#6b6868",
                overlayOpacity: 0.05,
                cornerRadius: 12,
                overlayBlur: 8,
                bodyOpacity: 0.78,
                borderThickness: 1,
                followOwner: true,
                closeOnOverlayClick: true,
                animationType: "ScaleUp",
                contentPosition: "Manual",
                marginTop: 30,
                marginLeft: 10,
                animationDurationMs: 100,
                bodyPadding: 0,
                content: ListViewTemplate.Create(
                    items: new List<string> { "退出窗口", "页面设置", "关于我们" },
                    onItemClick: (index, text) =>
                    {
                        if (index == 0) Environment.Exit(0);
                        else if (index == 1) { SwitchToPage("Settings"); ModalDialog.Dismiss(); }
                        else if (index == 2) { ModalDialog.Dismiss(); HelpSystem.ShowHelpWindow("关于我们"); }
                    },
                    width: 130,
                    itemHeight: 20,
                    fontSize: 14,
                    textColor: "#000000",
                    separatorColor: "#ccc1c1",
                    hoverColor: "#ececec",
                    fontWeightNumber: 400,
                    backgroundColor: "Transparent"
                )
            );
        };
        BtnPlus.Click += async (_, _) =>
        {
            await ModalDialog.Open(
                owner: this,
                width: 350,
                height: 600,
                backgroundColor: "#00FFFFFF",
                borderColor: "#00FFFFFF",
                overlayOpacity: 0.05,
                cornerRadius: 12,
                closeOnOverlayClick: false,
                animationType: "ScaleUp",
                animationDurationMs: 100,
                bodyPadding: 0.5,
                contentPosition: "Center",
                bodyOpacity: 1.0,
                content: ComponentLoader.Load("AddTerminal")
            );
        };

        Loaded += (_, _) =>
        {
            _media = new MediaManager(BackgroundLayer, this)
            {
                ImageOpacity = 1.0,
                VideoOpacity = 1.0,
                VideoStartDelayMs = 500,
                Brightness = 1.0
            };

            // 优先加载用户自定义壁纸，没有则用默认嘎
            string? savedWallpaper = Settings.LoadWallpaperPathStatic();
            if (!string.IsNullOrEmpty(savedWallpaper) && File.Exists(savedWallpaper))
            {
                ApplyStartupWallpaper(savedWallpaper);
            }
            else
            {
                _media.Load("ManiWindow");
            }

            if (!HasAgreed())
            {
                ShowAgreementWindow();
            }

            LoadCards();
        };

        SizeChanged += (_, _) => _media?.RefreshImageSize();
    }

    private void LoadCustomSlider()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "json_ui", "slider", "config.json");
        if (!File.Exists(configPath)) return;
        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<SliderConfig>(json);
        if (config == null) return;

        _slider = config.Style == "square" ? new SliderSquare() : new SliderRounded();
        SliderHost.Children.Clear();
        double trackHeight = CardScrollViewer.Bounds.Height > 0 ? CardScrollViewer.Bounds.Height : 380;
        double scrollMax = Math.Max(0, CardPanel.Bounds.Height - trackHeight);
        bool reversed = config.Direction == "reversed";

        if (_slider is SliderRounded rounded)
        {
            rounded.Configure(config.Width, config.Height, trackHeight, scrollMax, config.Speed, reversed, config.Color, config.Opacity, config.BorderRadius);
            rounded.ScrollChanged += t => CardScrollViewer.Offset = new Vector(CardScrollViewer.Offset.X, t);
        }
        else if (_slider is SliderSquare square)
        {
            square.Configure(config.Width, trackHeight, scrollMax, config.Speed, reversed, config.Color, config.Opacity);
            square.ScrollChanged += t => CardScrollViewer.Offset = new Vector(CardScrollViewer.Offset.X, t);
        }
        SliderHost.Children.Add(_slider);
        CardScrollViewer.ScrollChanged += (_, _) =>
        {
            if (_slider is SliderRounded r2) r2.UpdatePosition(CardScrollViewer.Offset.Y);
            else if (_slider is SliderSquare s2) s2.UpdatePosition(CardScrollViewer.Offset.Y);
        };
        CardScrollViewer.EffectiveViewportChanged += (_, _) =>
        {
            double nt = CardScrollViewer.Bounds.Height;
            double nm = Math.Max(0, CardPanel.Bounds.Height - nt);
            if (_slider is SliderRounded r3) r3.Configure(config.Width, config.Height, nt, nm, config.Speed, reversed, config.Color, config.Opacity, config.BorderRadius);
            else if (_slider is SliderSquare s3) s3.Configure(config.Width, nt, nm, config.Speed, reversed, config.Color, config.Opacity);
        };
    }

    private void GenerateCards(int count)
    {
        CardPanel.Children.Clear();
        for (int i = 0; i < count; i++)
        {
            var card = new Border
            {
                Width = CardWidth, Height = CardHeight,
                Background = new SolidColorBrush(BgColor),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(BorderThick),
                CornerRadius = new CornerRadius(CornerRadiusVal),
                Margin = new Thickness(CardGap / 2)
            };

            var iconPath = FindInIconLib("0006", ".png");
            var iconBtn = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Width = 80, Height = 80,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            iconBtn.Click += async (_, _) =>
            {
                await ModalDialog.Open(
                    owner: this,
                    width: 350,
                    height: 600,
                    backgroundColor: "#00FFFFFF",
                    borderColor: "#00FFFFFF",
                    overlayOpacity: 0.05,
                    cornerRadius: 12,
                    closeOnOverlayClick: false,
                    animationType: "ScaleUp",
                    animationDurationMs: 100,
                    bodyPadding: 0.5,
                    bodyOpacity: 1.0,  
                    contentPosition: "Center", 
                    content: ComponentLoader.Load("AddTerminal")
                );
            };
            if (iconPath != null)
                iconBtn.Content = new Image { Source = new Avalonia.Media.Imaging.Bitmap(iconPath), Width = 80, Height = 80 };

            var line1 = new TextBlock
            {
                Text = "欢迎使用TE终端盒子( ﹡ˆoˆ﹡ )",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#000000")),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var line2Panel = new StackPanel { Spacing = 2, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            line2Panel.Children.Add(new TextBlock { Text = "首次添加终端？", FontSize = 14, Foreground = new SolidColorBrush(Color.Parse("#000000")), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center });
            line2Panel.Children.Add(new TextBlock { Text = "点此参照官方文案", FontSize = 14, Foreground = new SolidColorBrush(Color.Parse("#0066ff")), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center });

            card.Child = null;
            var grid = new Grid();
            var stack = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Spacing = 12
            };
            stack.Children.Add(iconBtn);
            stack.Children.Add(line1);
            stack.Children.Add(line2Panel);
            grid.Children.Add(stack);
            card.Child = grid;
            card.Tag = "welcome";
            CardPanel.Children.Add(card);
        }
    }

    
    private static string? FindInIconLib(string fileName, params string[] extensions)
    {
        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "null";
        var exeDir = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var lib = Path.Combine(exeDir, "icon_lib");
        var found = Directory.Exists(lib);
        
        if (!found)
        {
            var d = baseDir;
            while (d != null && !File.Exists(Path.Combine(d, "TerminalEmbellish.Desktop.csproj")))
                d = Path.GetDirectoryName(d);
            if (d == null) d = baseDir;
            lib = Path.Combine(d, "icon_lib");
            found = Directory.Exists(lib);
        }

        if (!found) return null;
        foreach (var e in extensions) { var p = Path.Combine(lib, fileName + e); if (File.Exists(p)) return p; }
        return null;
    }

    private static string? FindHelpRoot()
    {
        var d = AppDomain.CurrentDomain.BaseDirectory;
        while (d != null && !File.Exists(Path.Combine(d, "TerminalEmbellish.Desktop.csproj")))
            d = Path.GetDirectoryName(d);
        if (d == null) d = AppDomain.CurrentDomain.BaseDirectory;
        var helpDir = Path.Combine(d, "help");
        if (!Directory.Exists(helpDir)) return null;
        return helpDir;
    }

     
    private int _terminalCount = 0;

    private Border BuildTerminalCard(string name, string shellType, string description, string imagePath, string backgroundPath = "")
    {
        var card = new Border
        {
            Width = CardWidth, Height = CardHeight,
            Background = new SolidColorBrush(BgColor),
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(BorderThick),
            CornerRadius = new CornerRadius(CornerRadiusVal),
            Margin = new Thickness(CardGap / 2)
        };

        var rootGrid = new Grid();

        var preview = new Border
        {
            Width = 340, Height = 100,
            CornerRadius = new CornerRadius(12),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(30, 6, 0, 0)
        };

        var previewGrid = new Grid();
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3, GridUnitType.Star)));
        previewGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(4, GridUnitType.Star)));

        var imgArea = new Border
        {
            Background = Brushes.Transparent,
            Margin = new Thickness(6),
            CornerRadius = new CornerRadius(16),
            BorderBrush = new SolidColorBrush(Color.Parse("#5e5e5e")),
            BorderThickness = new Thickness(1),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };

        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            imgArea.Child = new Image { Source = new Avalonia.Media.Imaging.Bitmap(imagePath), Stretch = Stretch.UniformToFill, Margin = new Thickness(2) };
        else
        {
            var placeholder = ImageLoader.Place("terminal", width: 40);
            if (placeholder != null) { placeholder.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center; placeholder.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center; imgArea.Child = placeholder; }
        }

        imgArea.PointerPressed += async (_, _) =>
        {
            var top = TopLevel.GetTopLevel(this) as Window;
            if (top == null) return;
            var files = await top.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "选择图片嘎",
                AllowMultiple = false,
                FileTypeFilter = new[] { new Avalonia.Platform.Storage.FilePickerFileType("图片") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg" } } }
            });
            if (files != null && files.Count > 0)
            {
                var path = files[0].Path.LocalPath;
                imgArea.Child = new Image { Source = new Avalonia.Media.Imaging.Bitmap(path), Stretch = Stretch.UniformToFill, Margin = new Thickness(2) };
                imgArea.Tag = path;
                if (card.Tag is CardData cd) cd.image = path;
            }
        };
        Grid.SetColumn(imgArea, 0);
        previewGrid.Children.Add(imgArea);

        var infoStack = new StackPanel { Margin = new Thickness(8, 8), Spacing = 6, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        var nameBlock = new TextBlock { Text = name, FontSize = 15, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#000000")) };
        var nameInput = new TextBox { Text = name, FontSize = 15, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#000000")), Background = new SolidColorBrush(Color.Parse("#EEEEEE")), IsVisible = false };

        nameBlock.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
            { nameBlock.IsVisible = false; nameInput.IsVisible = true; nameInput.Focus(); nameInput.CaretIndex = nameInput.Text.Length; }
        };
        nameInput.LostFocus += (_, _) => { nameBlock.Text = nameInput.Text; nameBlock.IsVisible = true; nameInput.IsVisible = false; if (card.Tag is CardData cd) cd.name = nameInput.Text; };

        var namePanel = new StackPanel(); namePanel.Children.Add(nameBlock); namePanel.Children.Add(nameInput);
        infoStack.Children.Add(namePanel);

        var descBlock = new TextBlock { Text = string.IsNullOrEmpty(description) ? "暂无简介" : description, FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#888888")), TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var descInput = new TextBox { Text = string.IsNullOrEmpty(description) ? "暂无简介" : description, FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#888888")), Background = new SolidColorBrush(Color.Parse("#EEEEEE")), IsVisible = false, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MinHeight = 40 };

        descBlock.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
            { descBlock.IsVisible = false; descInput.IsVisible = true; descInput.Focus(); }
        };
        descInput.LostFocus += (_, _) => { descBlock.Text = descInput.Text; descBlock.IsVisible = true; descInput.IsVisible = false; if (card.Tag is CardData cd) cd.description = descInput.Text; };

        var descPanel = new StackPanel(); descPanel.Children.Add(descBlock); descPanel.Children.Add(descInput);
        infoStack.Children.Add(descPanel);

        rootGrid.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(null).Properties.IsRightButtonPressed) return;
            if (nameInput.IsVisible) { nameBlock.Text = nameInput.Text; nameBlock.IsVisible = true; nameInput.IsVisible = false; if (card.Tag is CardData cd) cd.name = nameInput.Text; }
            if (descInput.IsVisible) { descBlock.Text = descInput.Text; descBlock.IsVisible = true; descInput.IsVisible = false; if (card.Tag is CardData cd) cd.description = descInput.Text; }
        };

        Grid.SetColumn(infoStack, 1);
        previewGrid.Children.Add(infoStack);
        preview.Child = previewGrid;
        rootGrid.Children.Add(preview);

        var btnPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 8, 8) };

        var delBtn = CreateIconButton("0001a", 24);
        delBtn.Click += (_, _) => { CardPanel.Children.Remove(card); _terminalCount--; SaveAllCards(); };
        btnPanel.Children.Add(delBtn);
        var saveBtn = CreateIconButton("0002a", 24);
        saveBtn.Click += (_, _) => SaveAllCards();
        btnPanel.Children.Add(saveBtn);

        var settingsBtn = CreateIconButton("0003a", 24);
        settingsBtn.Click += async (_, _) =>
        {
            var currentCardData = card.Tag as CardData;

            var previewBorder = new Border
            {
                Width = 280, Height = 158,
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true,
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Child = CreatePreviewFromPath(currentCardData?.background ?? "")
            };

            previewBorder.PointerPressed += async (s, ev) =>
            {
                var top = TopLevel.GetTopLevel(this) as Window;
                if (top == null || currentCardData == null) return;
                var files = await top.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "选择背景（图片或视频）",
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
                    currentCardData.background = path;
                    previewBorder.Child = CreatePreviewFromPath(path);
                    SaveAllCards();
                }
            };

            await ModalDialog.Open(
                owner: this,
                width: 350,
                height: 600,
                backgroundColor: "#CCFFFFFF",
                borderColor: "#00FFFFFF",
                overlayOpacity: 0.05,
                cornerRadius: 12,
                closeOnOverlayClick: true,
                animationType: "ScaleUp",
                animationDurationMs: 100,
                contentPosition: "Center",
                bodyOpacity: 1.0,
                content: new StackPanel { Spacing = 0, Children = {
                    new TextBlock { Text = "设置", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#000000")), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 16) },
                    VerticalList.Create(
                        items: new List<Control> { previewBorder },
                        sectionTitle: "背景样式", titleAlignment: "Left",
                        itemHeight: 192, spacing: 0, top: 0, left: 16, right: 16, cornerRadius: 22, backgroundColor: "#00FFFFFF", borderColor: "#00FFFFFF", borderThickness: 0),
                    VerticalList.Create(
                        items: new List<Control> { new TextBlock { Text = "由于技术原因，目前暂不支持更改", FontSize = 14, Foreground = new SolidColorBrush(Color.Parse("#000000")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center } },
                        sectionTitle: "文字样式", titleAlignment: "Left",
                        itemHeight: 48, spacing: 0, top: 8, left: 16, right: 16, cornerRadius: 22, backgroundColor: "#99FFFFFF", borderColor: "#00FFFFFF", borderThickness: 0),
                    VerticalList.Create(
                        items: new List<Control> { new TextBlock { Text = "由于技术原因，目前暂不支持更改", FontSize = 14, Foreground = new SolidColorBrush(Color.Parse("#000000")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center } },
                        sectionTitle: "设置字体", titleAlignment: "Left",
                        itemHeight: 48, spacing: 0, top: 8, left: 16, right: 16, cornerRadius: 22, backgroundColor: "#99FFFFFF", borderColor: "#00FFFFFF", borderThickness: 0)
                } }
            );
        };
        btnPanel.Children.Add(settingsBtn);

        var powerBtn = new Button { Width = 24, Height = 24, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
        var powerIcon = ImageLoader.Place("off", width: 24);
        if (powerIcon != null) powerBtn.Content = powerIcon;
        powerBtn.Click += (_, _) =>
        {
            var win = powerBtn.Tag as Window;
            if (win != null) { win.Close(); powerBtn.Tag = null; var oi = ImageLoader.Place("off", width: 24); if (oi != null) powerBtn.Content = oi; }
            else
            {
                var oi = ImageLoader.Place("on", width: 24); if (oi != null) powerBtn.Content = oi;
                var bgPath = (card.Tag is CardData cd) ? cd.background : "";
                win = new Window { Title = "TerminalEmbellish 终端", Width = 1800, Height = 880, Content = new TerminalEmbellish.page_ui.Terminal.TerminalWindow(shellType, bgPath), WindowStartupLocation = WindowStartupLocation.CenterOwner, WindowDecorations = WindowDecorations.None };
                win.Closed += (_, _) => { powerBtn.Tag = null; var ofi = ImageLoader.Place("off", width: 24); if (ofi != null) powerBtn.Content = ofi; };
                powerBtn.Tag = win; win.Show();
            }
        };
        btnPanel.Children.Add(powerBtn);
        rootGrid.Children.Add(btnPanel);

        card.Child = rootGrid;
        card.Tag = new CardData { name = name, type = shellType, description = description, image = imagePath, background = backgroundPath };
        return card;
    }

    // ===== 背景预览相关方法 =====

    private Image CreatePreviewFromPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return CreateDefaultPreview();

        var ext = Path.GetExtension(path).ToLower();
        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
        {
            return new Image
            {
                Source = new Avalonia.Media.Imaging.Bitmap(path),
                Width = 280, Height = 158,
                Stretch = Stretch.UniformToFill
            };
        }
        else if (ext == ".mp4" || ext == ".avi" || ext == ".webm")
        {
            return CreateVideoFramePreview(path, 1);
        }

        return CreateDefaultPreview();
    }

    private Image CreateVideoFramePreview(string videoPath, int frameNumber)
    {
        var framesDir = Path.Combine(Path.GetTempPath(), "TE_Card_Previews");
        Directory.CreateDirectory(framesDir);
        var hash = Math.Abs(videoPath.GetHashCode()).ToString("X8");
        var outputPath = Path.Combine(framesDir, $"{hash}_f{frameNumber}.png");

        if (!File.Exists(outputPath))
        {
            var ff = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-y -i \"{videoPath}\" -vf \"select=eq(n\\,{frameNumber - 1})\" -vframes 1 -q:v 3 \"{outputPath}\"",
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                }
            };
            ff.Start();
            ff.WaitForExit();
        }

        if (File.Exists(outputPath))
        {
            return new Image
            {
                Source = new Avalonia.Media.Imaging.Bitmap(outputPath),
                Width = 280, Height = 158,
                Stretch = Stretch.UniformToFill
            };
        }

        return new Image { Width = 280, Height = 158, Stretch = Stretch.Uniform };
    }

    private Image CreateDefaultPreview()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "TerminalEmbellish.Desktop.csproj")))
            dir = Path.GetDirectoryName(dir);
        var root = dir ?? AppDomain.CurrentDomain.BaseDirectory;
        var defaultVideo = Path.Combine(root, "te.mp4");
        if (File.Exists(defaultVideo))
            return CreateVideoFramePreview(defaultVideo, 1);

        return new Image { Width = 280, Height = 158, Stretch = Stretch.Uniform };
    }

    // ===== 卡片管理方法 =====

    public void AddOrReplaceCard(string name, string shellType = "cmd", string description = "", string imagePath = "")
    {
        _terminalCount++;
        if (_terminalCount == 1 && CardPanel.Children.Count > 0) CardPanel.Children.RemoveAt(0);
        var card = BuildTerminalCard(name, shellType, description, imagePath);
        CardPanel.Children.Add(card);
    }

    private void SaveAllCards()
    {
        var cardsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".TerminalEmbellish", "cards");
        Directory.CreateDirectory(cardsDir);
        foreach (var f in Directory.GetFiles(cardsDir, "card_*.json")) File.Delete(f);
        int i = 1;
        int savedCount = 0;
        foreach (var child in CardPanel.Children)
        {
            if (child is Border b && b.Tag is CardData cd)
            {
                var json = JsonSerializer.Serialize(cd);
                File.WriteAllText(Path.Combine(cardsDir, $"card_{i:D3}.json"), json);
                i++; savedCount++;
            }
        }
        _terminalCount = i - 1;
    }

    private void LoadCards()
    {
        var cardsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".TerminalEmbellish", "cards");
        if (!Directory.Exists(cardsDir)) return;
        var files = Directory.GetFiles(cardsDir, "card_*.json").OrderBy(f => f).ToArray();
        if (files.Length > 0) CardPanel.Children.Clear();
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var cd = JsonSerializer.Deserialize<CardData>(json);
            if (cd != null)
            {
                _terminalCount++;
                var card = BuildTerminalCard(cd.name, cd.type, cd.description, cd.image, cd.background);
                CardPanel.Children.Add(card);
            }
        }
    }

    public class CardData
    {
        public string name { get; set; } = "";
        public string type { get; set; } = "cmd";
        public string description { get; set; } = "";
        public string image { get; set; } = "";
        public string background { get; set; } = "";
    }

    private Border CreateImageBorder()
    {
        return new Border
        {
            Width = 90, Height = 50,
            CornerRadius = new CornerRadius(8),
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(6),
            Child = new TextBlock { Text = "🖼", FontSize = 18, Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center }
        };
    }

    private Button CreateIconButton(string iconName, double size)
    {
        var btn = new Button
        {
            Width = size, Height = size,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        var icon = ImageLoader.Place(iconName, width: size);
        if (icon != null) btn.Content = icon;
        return btn;
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        double winW = e.NewSize.Width;
        int perRow = Math.Max(1, (int)((winW - SidePadding * 2) / (CardWidth + CardGap)));
        double rowW = perRow * CardWidth + (perRow - 1) * CardGap;
        double offset = Math.Max(0, (winW - SidePadding * 2 - rowW) / 2);
        CardPanel.Margin = new Thickness(SidePadding + offset, 10, SidePadding, 0);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (Instance == this)
            Instance = null;
        _media?.Dispose();
        base.OnClosed(e);
    }

    private bool HasAgreed()
    {
        if (!File.Exists(AgreementPath)) return false;
        try
        {
            var json = File.ReadAllText(AgreementPath);
            using var doc = JsonDocument.Parse(json);
            var version = doc.RootElement.GetProperty("version").GetString() ?? "";
            return version == "v1.0.0-beta";
        }
        catch { return false; }
    }

    private void ShowAgreementWindow()
    {
        var window = new Window
        {
            Title = "用户协议",
            Width = 550, Height = 650,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowDecorations = WindowDecorations.Full,
            Background = Brushes.White
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(60)));

        var browser = new TerminalEmbellish.templates.@private.BrowserTemplate.BrowserControl();
        Grid.SetRow(browser, 0);

        var bottomBar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#FAFAFA")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 0)
        };
        var bottomStack = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        var chkAgree = new CheckBox
        {
            Content = "我已阅读并同意《用户隐私与协议》",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.Parse("#333333")),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var btnStart = new Button
        {
            Content = "开始使用",
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Width = 90, Height = 34,
            Background = new SolidColorBrush(Color.Parse("#4A9EFF")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(6),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            IsEnabled = false
        };

        chkAgree.IsCheckedChanged += (_, _) =>
        {
            btnStart.IsEnabled = chkAgree.IsChecked == true;
        };

        btnStart.Click += (_, _) =>
        {
            var dir = Path.GetDirectoryName(AgreementPath);
            if (dir != null) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new { agreed = true, version = "v1.0.0-beta", timestamp = DateTime.Now.ToString("O") });
            File.WriteAllText(AgreementPath, json);

            window.Close();
        };

        window.Closing += (_, e) =>
        {
            if (!HasAgreed())
            {
                Environment.Exit(0);
            }
        };

        bottomStack.Children.Add(chkAgree);
        bottomStack.Children.Add(btnStart);
        bottomBar.Child = bottomStack;
        Grid.SetRow(bottomBar, 1);

        grid.Children.Add(browser);
        grid.Children.Add(bottomBar);
        window.Content = grid;

        var helpRoot = FindHelpRoot();
        var htmlPath = helpRoot != null ? Path.Combine(helpRoot, "关于我们", "用户隐私与协议.html") : null;
        window.Loaded += (_, _) =>
        {
            if (htmlPath != null)
            {
                try
                {
                    browser.LoadHtmlFile(htmlPath);
                }
                catch
                {
                    browser.Content = new TextBlock
                    {
                        Text = "协议内容加载失败，请检查文件是否存在。",
                        Foreground = new SolidColorBrush(Color.Parse("#333333")),
                        FontSize = 14,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };
                }
            }
            else
            {
                browser.Content = new TextBlock
                {
                    Text = "协议文件未找到。",
                    Foreground = new SolidColorBrush(Color.Parse("#333333")),
                    FontSize = 14,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
            }
        };

        window.ShowDialog(this);
    }

    public void SwitchToPage(string pageName)
    {
        _mainContent = Content as Control;
        var page = ComponentLoader.Load(pageName);
        if (page == null) return;
        Content = page;
    }

    public void GoBackToMain()
    {
        if (_mainContent != null) Content = _mainContent;
    }

    public Panel BgLayer => BackgroundLayer;

    public void ApplyStartupWallpaper(string path)
    {
        if (!File.Exists(path)) return;

        var ext = Path.GetExtension(path).ToLower();
        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
        {
            BackgroundLayer.Children.Clear();
            var bmp = new Avalonia.Media.Imaging.Bitmap(path);
            var img = new Image
            {
                Source = bmp,
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Opacity = 1.0
            };
            BackgroundLayer.Children.Add(img);
        }
        else if (ext == ".mp4" || ext == ".avi" || ext == ".webm")
        {
            var destDir = AppDomain.CurrentDomain.BaseDirectory;
            var destName = "CustomWallpaper" + ext;
            var destPath = Path.Combine(destDir, destName);
            try
            {
                File.Copy(path, destPath, true);
                _media?.Dispose();
                _media = new MediaManager(BackgroundLayer, this)
                {
                    ImageOpacity = 1.0,
                    VideoOpacity = 1.0,
                    VideoStartDelayMs = 500,
                    Brightness = 1.0
                };
                _media.Load("CustomWallpaper");
            }
            catch { }
        }
    }

    public void ResetToDefaultWallpaper()
    {
        BackgroundLayer.Children.Clear();
        _media?.Dispose();
        _media = new MediaManager(BackgroundLayer, this)
        {
            ImageOpacity = 1.0,
            VideoOpacity = 1.0,
            VideoStartDelayMs = 500,
            Brightness = 1.0
        };
        _media.Load("ManiWindow");
        Tag = _media;
    }
}

public class SliderConfig
{
    public string Position { get; set; } = "right";
    public double Width { get; set; } = 10;
    public double Height { get; set; } = 60;
    public double MinHeight { get; set; } = 30;
    public double MaxHeight { get; set; } = 200;
    public string Direction { get; set; } = "natural";
    public string Style { get; set; } = "rounded";
    public double Speed { get; set; } = 1.0;
    public string Color { get; set; } = "#555555";
    public string HoverColor { get; set; } = "#888888";
    public double BorderRadius { get; set; } = 5;
    public double Opacity { get; set; } = 0.8;
}