#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using TerminalEmbellish.templates.@private.Behaviors;

namespace TerminalEmbellish.page_ui.Terminal;

public partial class TerminalWindow : UserControl
{
    private Image? _bgImage;
    private Timer? _frameTimer;
    private int _frameIndex;
    private string? _framesDir;
    private string _shellType;
    private string _backgroundPath = "";
    private Process? _shellProcess;
    private string _currentInput = "";

    public TerminalWindow() : this("cmd") { }

    public TerminalWindow(string shellType, string backgroundPath = "")
    {
        _shellType = shellType;
        _backgroundPath = backgroundPath;
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            DragHelper.EnableDrag(DragArea);

            var topLevel = TopLevel.GetTopLevel(this) as Window;
            if (topLevel != null)
            {
                DragHelper.EnableResize(topLevel);
                topLevel.Title = _shellType == "powershell" ? "PowerShell" : "CMD";
            }

            LoadVideo();

            await Task.Delay(200);
            if (topLevel != null)
            {
                double h = topLevel.Height;
                topLevel.Width = h * 16.0 / 9.0;
            }

            // 直接启动 shell，shell 自己会输出版权信息嘎
            if (_shellType == "powershell")
                StartShell("powershell.exe", "-NoLogo");
            else
                StartShell("cmd.exe", "");

            // 键盘监听嘎
            this.Focusable = true;
            this.Focus();
            this.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    if (!string.IsNullOrEmpty(_currentInput))
                    {
                        TerminalOutput.Text += "\n";
                        _shellProcess?.StandardInput.WriteLine(_currentInput);
                        _currentInput = "";
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.Back)
                {
                    if (_currentInput.Length > 0)
                    {
                        _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
                        var t = TerminalOutput.Text;
                        if (t.Length > 0) TerminalOutput.Text = t.Substring(0, t.Length - 1);
                    }
                    e.Handled = true;
                }
                else if (e.KeySymbol != null && e.KeySymbol.Length == 1 && !char.IsControl(e.KeySymbol[0]))
                {
                    _currentInput += e.KeySymbol;
                    TerminalOutput.Text += e.KeySymbol;
                }
};

             this.AddHandler(PointerPressedEvent, async (_, e) =>
             {
                 if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                 {
                     var topLevel = TopLevel.GetTopLevel(this);
                     if (topLevel?.Clipboard != null && _shellProcess != null)
                     {
                         var clipboardText = await topLevel.Clipboard.TryGetTextAsync();
                         if (!string.IsNullOrEmpty(clipboardText))
                         {
                             _shellProcess.StandardInput.Write(clipboardText);
                             TerminalOutput.Text += clipboardText;
                             _currentInput += clipboardText;
                         }
                     }
                     e.Handled = true;
                 }
             }, Avalonia.Interactivity.RoutingStrategies.Tunnel);

             TerminalOutput.PointerReleased += async (_, e) =>
             {
                 if (e.GetCurrentPoint(TerminalOutput).Properties.IsLeftButtonPressed)
                 {
                     var selectedText = TerminalOutput.SelectedText;
                     if (!string.IsNullOrEmpty(selectedText))
                     {
                         var clipboard = TopLevel.GetTopLevel(TerminalOutput)?.Clipboard;
                         if (clipboard != null)
                         {
                             await clipboard.SetTextAsync(selectedText);
                         }
                     }
                 }
             };
         };
    }

    private void StartShell(string fileName, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Console.OutputEncoding,
                StandardErrorEncoding = Console.OutputEncoding
            };

            _shellProcess = Process.Start(psi);
            if (_shellProcess == null) return;

            _shellProcess.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Dispatcher.UIThread.Post(() =>
                    {
                        TerminalOutput.Text += e.Data + "\n";
                    });
            };
            _shellProcess.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Dispatcher.UIThread.Post(() =>
                    {
                        TerminalOutput.Text += e.Data + "\n";
                    });
            };
            _shellProcess.BeginOutputReadLine();
            _shellProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => TerminalOutput.Text += "启动失败: " + ex.Message + "\n");
        }
    }

    private void LoadVideo()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "TerminalEmbellish.Desktop.csproj")))
            dir = Path.GetDirectoryName(dir);
        var root = dir ?? AppDomain.CurrentDomain.BaseDirectory;

        string sourcePath;
        if (!string.IsNullOrEmpty(_backgroundPath) && File.Exists(_backgroundPath))
            sourcePath = _backgroundPath;
        else
            sourcePath = Path.Combine(root, "te.mp4");

        if (!File.Exists(sourcePath)) return;

        var ext = Path.GetExtension(sourcePath).ToLower();

        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
        {
            _bgImage = new Image
            {
                Source = new Bitmap(sourcePath),
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Opacity = 1.0
            };
            RootGrid.Children.Insert(0, _bgImage);
            return;
        }

        _framesDir = Path.Combine(Path.GetTempPath(), "TE_Terminal_frames");
        if (Directory.Exists(_framesDir)) Directory.Delete(_framesDir, true);
        Directory.CreateDirectory(_framesDir);

        Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -i \"{sourcePath}\" -vf \"fps=10,scale=960:-1\" -q:v 5 \"{Path.Combine(_framesDir, "frame_%04d.png")}\"",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        })?.WaitForExit();

        _bgImage = new Image
        {
            Stretch = Stretch.UniformToFill,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Opacity = 1.0
        };
        RootGrid.Children.Insert(0, _bgImage);

        _frameIndex = 0;
        _frameTimer = new Timer(100);
        _frameTimer.Elapsed += (_, _) => Dispatcher.UIThread.Post(() => NextFrame());
        _frameTimer.Start();
    }

    private void NextFrame()
    {
        if (_framesDir == null || _bgImage == null) return;
        var files = Directory.GetFiles(_framesDir, "frame_*.png").OrderBy(f => f).ToArray();
        if (files.Length == 0) return;
        _frameIndex = (_frameIndex + 1) % files.Length;
        _bgImage.Source = new Bitmap(files[_frameIndex]);
    }
}



