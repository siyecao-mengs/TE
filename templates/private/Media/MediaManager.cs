using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace TerminalEmbellish.templates.@private.Media
{
    public class MediaManager
    {
        private readonly Panel _host;
        private readonly Window _window;
        private Image? _bgImage;
        private Timer? _frameTimer;
        private int _frameIndex;
        private string? _framesDir;
        private bool _isVideoPlaying;

        public double ImageOpacity { get; set; } = 0.5;
        public double VideoOpacity { get; set; } = 0.3;
        public double BlurRadius { get; set; } = 0;
        public double Brightness { get; set; } = 1.0;
        public int VideoStartDelayMs { get; set; } = 3000;
        public int VideoFps { get; set; } = 10;
        public int VideoScaleWidth { get; set; } = 960;

        public MediaManager(Panel host, Window window)
        {
            _host = host;
            _window = window;
        }

        public void Load(string fileName)
        {
            LoadImage(fileName);
            var timer = new Timer(VideoStartDelayMs) { AutoReset = false };
            timer.Elapsed += (_, _) =>
            {
                Dispatcher.UIThread.Post(() => LoadVideo(fileName));
                timer.Dispose();
            };
            timer.Start();
        }

        private void LoadImage(string fileName)
        {
            var path = FindFile(fileName, ".jpg", ".jpeg", ".png");
            if (path == null) return;
            try
            {
                var bmp = new Bitmap(path);
                _bgImage = new Image
                {
                    Source = bmp,
                    Stretch = Stretch.Fill,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                    Opacity = ImageOpacity
                };
                Dispatcher.UIThread.Post(() => _host.Children.Add(_bgImage));
            }
            catch (Exception)
            {
                _bgImage = null;
                return;
            }
            if (BlurRadius > 0 && _bgImage != null) _bgImage.Effect = new BlurEffect { Radius = BlurRadius };
            if (Brightness < 1.0)
            {
                var dimOverlay = new Border
                {
                    Background = new SolidColorBrush(Avalonia.Media.Colors.Black, 1.0 - Brightness),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
                };
                _host.Children.Add(dimOverlay);
            }
            RefreshImageSize();
        }

        private void LoadVideo(string fileName)
        {
            var path = FindFile(fileName, ".mp4", ".avi", ".webm");
            if (path == null) return;
            try
            {
                _framesDir = Path.Combine(Path.GetTempPath(), "TerminalEmbellish_frames");
                Directory.CreateDirectory(_framesDir);
                var ff = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-y -i \"{path}\" -vf \"fps={VideoFps},scale={VideoScaleWidth}:-1\" -q:v 5 \"{Path.Combine(_framesDir, "frame_%04d.png")}\"",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                if (ff != null)
                {
                    ff.WaitForExit();
                }
                else
                {
                    return;
                }

                _isVideoPlaying = true;
                if (_bgImage != null) { Dispatcher.UIThread.Post(() => _bgImage.Opacity = VideoOpacity); }
                _frameIndex = 0;
                _frameTimer = new Timer(1000.0 / VideoFps);
                _frameTimer.Elapsed += (_, _) => { Dispatcher.UIThread.Post(() => NextFrame()); };
                _frameTimer.Start();
            }
            catch (Exception)
            {
                _isVideoPlaying = false;
                return;
            }
        }

        private void NextFrame()
        {
            if (_framesDir == null || _bgImage == null) return;
            var files = Directory.GetFiles(_framesDir, "frame_*.png").OrderBy(f => f).ToArray();
            if (files.Length == 0) return;
            _frameIndex = (_frameIndex + 1) % files.Length;
            _bgImage.Source = new Bitmap(files[_frameIndex]);
        }

        public void RefreshImageSize()
        {
            if (_bgImage?.Source is not Bitmap bmp) return;
            double iw = bmp.PixelSize.Width, ih = bmp.PixelSize.Height;
            double cw = _window.Width > 0 ? _window.Width : 460;
            double ch = _window.Height > 0 ? _window.Height : 880;
            double s = Math.Max(cw / iw, ch / ih);
            _bgImage.Width = iw * s;
            _bgImage.Height = ih * s;
            _bgImage.Margin = new Thickness((cw - _bgImage.Width) / 2, (ch - _bgImage.Height) / 2, 0, 0);
        }

        public void Dispose()
        {
            _frameTimer?.Stop(); _frameTimer?.Dispose();
            if (_framesDir != null && Directory.Exists(_framesDir))
                Directory.Delete(_framesDir, true);
        }

        private string? FindFile(string fileName, params string[] extensions)
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            var dir = string.IsNullOrEmpty(exePath) ? AppDomain.CurrentDomain.BaseDirectory : Path.GetDirectoryName(exePath)!;
            while (dir != null && !File.Exists(Path.Combine(dir, "TerminalEmbellish.Desktop.csproj")))
                dir = Directory.GetParent(dir)?.FullName;
            if (dir == null) dir = AppDomain.CurrentDomain.BaseDirectory;
            foreach (var ext in extensions)
            {
                var p = Path.Combine(dir, fileName + ext);
                if (File.Exists(p)) return p;
            }
            return null;
        }
    }
}
