#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using TerminalEmbellish.templates.Components;
using TerminalEmbellish.help;
using TerminalEmbellish.templates.@private.Behaviors;
using TerminalEmbellish.templates.@private.Media;

namespace TerminalEmbellish.page_ui.Dialogs;

public partial class AddTerminal : UserControl
{
    private string selectedTerminal = "";
    private bool canAdd = false;

    public AddTerminal()
    {
        InitializeComponent();
        MainBorder.Background = new SolidColorBrush(Color.Parse("#CCFFFFFF"));

        Loaded += (_, _) =>
        {
            var localPanel = new StackPanel { Spacing = 12, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) };

            var titleRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            titleRow.Children.Add(new TextBlock { Text = "Windows 10/11 已收录终端", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#000000")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            var helpBtn = HelpSystem.CreateHelpButton(relativePath: "使用说明/什么是已收录终端.html", buttonSize: 20);
            titleRow.Children.Add(helpBtn);
            localPanel.Children.Add(titleRow);

            var terminalIcons = new[] { "cmd", "powershell", "0002add" };
            int columns = 3; double cardSize = 80; double iconSize = 36;
            var cardGrid = new WrapPanel { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, MaxWidth = columns * (cardSize + 12), Margin = new Thickness(0, 0, 0, 10) };
            var allCards = new List<Border>();

            var infoText = new TextBlock { FontSize = 13, Foreground = new SolidColorBrush(Color.Parse("#555555")), TextAlignment = Avalonia.Media.TextAlignment.Center, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Thickness(30, 8, 30, 0), Text = "请选择上方终端类型\n再点击右下角按钮添加终端" };

            foreach (var iconName in terminalIcons)
            {
                var card = new Border { Width = cardSize, Height = cardSize, CornerRadius = new CornerRadius(20), BorderBrush = new SolidColorBrush(Color.Parse("#969696")), BorderThickness = new Thickness(1.5), Background = new SolidColorBrush(Color.Parse("#F5F5F5")), Margin = new Thickness(6), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
                var icon = ImageLoader.Place(iconName, width: iconSize);
                if (icon != null) { icon.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center; icon.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center; card.Child = icon; }
                var capIconName = iconName;
                card.PointerPressed += (_, _) =>
                {
                    foreach (var c in allCards) c.BorderBrush = new SolidColorBrush(Color.Parse("#969696"));
                    card.BorderBrush = new SolidColorBrush(Color.Parse("#FFD700"));
                    selectedTerminal = capIconName;
                    canAdd = true;
                    if (capIconName == "cmd") infoText.Text = "CMD\nWindows经典终端\n批处理脚本，快速系统操作\n\n点击右下角按钮添加终端";
                    else if (capIconName == "powershell") infoText.Text = "PowerShell\n微软现代终端\n对象管道，自动化脚本\n\n点击右下角按钮添加终端";
                    else { infoText.Text = "此终端类型暂未收录！"; canAdd = false; }
                };
                allCards.Add(card); cardGrid.Children.Add(card);
            }
            localPanel.Children.Add(cardGrid);
            localPanel.Children.Add(infoText);

            var remotePanel = new StackPanel { Spacing = 16, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
            remotePanel.Children.Add(new Border { Height = 20, Background = Brushes.Transparent });
            var avatar = ImageLoader.Place("siyecao", width: 80);
            if (avatar != null) { avatar.Clip = new EllipseGeometry(new Rect(0, 0, 80, 80)); avatar.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center; remotePanel.Children.Add(avatar); }
            remotePanel.Children.Add(new TextBlock { Text = "UP正在努力开发中\n敬请期待...", FontSize = 14, Foreground = new SolidColorBrush(Color.Parse("#969696")), TextAlignment = Avalonia.Media.TextAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center });

            var tabs = SlidingTab.Create(tabs: new() { ("本地终端", localPanel), ("远控终端", remotePanel) }, width: 350, height: 550, tabWidth: 100, fontSize: 14, textColor: "#000000", indicatorColor: "#969696");
            ContentArea.Children.Add(tabs);

            var addBtn = new Button { Width = 60, Height = 60, ZIndex = 100, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 10, 10), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
            var addIcon = ImageLoader.Place("0001add", width: 60);
            if (addIcon != null) addBtn.Content = addIcon;
            addBtn.Click += async (_, _) =>
            {
                if (!canAdd) return;

                var currentWindow = TopLevel.GetTopLevel(this) as Window;
                var mainWin = currentWindow?.Owner as TerminalEmbellish.MainWindow ?? TopLevel.GetTopLevel(this) as TerminalEmbellish.MainWindow;
                currentWindow?.Close();
                await Task.Delay(200);

                if (mainWin != null)
                {
                    string shellType = selectedTerminal == "powershell" ? "powershell" : "cmd";
                    string cardName = selectedTerminal == "powershell" ? "PowerShell" : "CMD";
                    mainWin.AddOrReplaceCard(cardName, shellType);
                }
            };
            var rootGrid = this.FindControl<Grid>("RootGrid");
            if (rootGrid != null) rootGrid.Children.Add(addBtn);
        };
    }
}


