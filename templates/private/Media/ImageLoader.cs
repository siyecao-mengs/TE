using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace TerminalEmbellish.templates.@private.Media
{
    public static class ImageLoader
    {
        private static bool _imgDbg = false;

        public static Image? Place(
            string fileName,
            double width = 80,
            double height = 0,
            double? left = null,
            double? top = null,
            double? right = null,
            double? bottom = null,
            double opacity = 1.0)
        {
            var path = FindInIconLib(fileName);
            if (path == null) return null;

            var bmp = new Bitmap(path);
            double h = height > 0 ? height : (width / bmp.PixelSize.Width * bmp.PixelSize.Height);

            double marginLeft = left ?? 0;
            double marginTop = top ?? 0;
            double marginRight = right ?? 0;
            double marginBottom = bottom ?? 0;

            var img = new Image
            {
                Source = bmp,
                Width = width,
                Height = h,
                Opacity = opacity,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(marginLeft, marginTop, marginRight, marginBottom)
            };

            if (left != null && right != null)
                img.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            else if (right != null)
                img.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
            else if (left != null)
                img.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            else
                img.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;

            if (top != null && bottom != null)
                img.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
            else if (bottom != null)
                img.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
            else if (top != null)
                img.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            else
                img.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;

            return img;
        }

        private static string? FindInIconLib(string fileName)
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            var exeDir = string.IsNullOrEmpty(exePath) ? AppDomain.CurrentDomain.BaseDirectory : Path.GetDirectoryName(exePath)!;
            var libDir = Path.Combine(exeDir, "icon_lib");
            if (!Directory.Exists(libDir))
            {
                var dir = AppDomain.CurrentDomain.BaseDirectory;
                while (dir != null && !File.Exists(Path.Combine(dir, "TerminalEmbellish.Desktop.csproj")))
                    dir = Path.GetDirectoryName(dir);
                if (dir == null) dir = AppDomain.CurrentDomain.BaseDirectory;
                libDir = Path.Combine(dir, "icon_lib");
            }
            if (!Directory.Exists(libDir)) return null;
            foreach (var ext in new[] { ".png", ".jpg", ".jpeg", ".svg" })
            {
                var p = Path.Combine(libDir, fileName + ext);
                if (File.Exists(p)) return p;
            }
            var full = Path.Combine(libDir, fileName);
            if (File.Exists(full)) return full;
            return null;
        }
    }
}
