using System;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace TerminalEmbellish.templates.@private.BrowserTemplate
{
    public interface IHtmlBrowser
    {
        Control Control { get; }
        void LoadHtmlFile(string htmlPath);
        void LoadHtml(string html);
        void LoadUrl(string url);
        void GetScrollInfo(Action<double, double, double> callback);
        void ScrollTo(double y);
        void SetOffscreenMode(bool enabled);
    }

    public class BrowserControl : UserControl, IHtmlBrowser
    {
        private NativeWebView? _webView;
        private bool _isInitialized;
        private string? _pendingHtml;
        private string? _pendingHtmlPath;
        private Uri? _pendingUri;
        private string? _tempHtmlFilePath;

        public Control Control => this;

        public BrowserControl()
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
            ClipToBounds = true;

            AttachedToVisualTree += (_, _) => InitializeBrowser();
        }

        private void InitializeBrowser()
        {
            if (_isInitialized) return;

            try
            {
                _webView = new NativeWebView
                {
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                };

                Content = _webView;
                _isInitialized = true;

                if (_pendingHtmlPath != null)
                {
                    LoadHtmlFileInternal(_pendingHtmlPath);
                    _pendingHtmlPath = null;
                }
                else if (_pendingHtml != null)
                {
                    LoadHtmlInternal(_pendingHtml);
                    _pendingHtml = null;
                }
                else if (_pendingUri != null)
                {
                    LoadUrl(_pendingUri.ToString());
                    _pendingUri = null;
                }
            }
            catch (Exception ex)
            {
                var msg = $"WebView 初始化失败: {ex.Message}\n\n请确保当前平台已安装对应的 WebView 运行时。";
                Console.WriteLine($"[WebView] {msg}");
                Content = new TextBlock
                {
                    Text = msg,
                    Foreground = Brushes.Red,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(20)
                };
            }
        }

        public void LoadHtmlFile(string htmlPath)
        {
            if (!_isInitialized || _webView == null)
            {
                _pendingHtmlPath = htmlPath;
                _pendingHtml = null;
                _pendingUri = null;
                return;
            }
            LoadHtmlFileInternal(htmlPath);
        }

        private void LoadHtmlFileInternal(string htmlPath)
        {
            if (!File.Exists(htmlPath))
            {
                Console.WriteLine($"[WebView] HTML 文件不存在: {htmlPath}");
                return;
            }

            var html = File.ReadAllText(htmlPath);
            var htmlDir = Path.GetDirectoryName(htmlPath) ?? "";
            html = InjectBaseHref(html, htmlDir);
            html = InjectScrollbarCss(html);

            var tempFileName = Path.GetFileName(htmlPath);
            var cacheDir = Path.Combine(Path.GetTempPath(), "TerminalEmbellish_HtmlViewer");
            Directory.CreateDirectory(cacheDir);
            var tempFile = Path.Combine(cacheDir, $"__TerminalEmbellish_Temp_{Guid.NewGuid():N}_{tempFileName}");
            File.WriteAllText(tempFile, html);
            _tempHtmlFilePath = tempFile;

            Dispatcher.UIThread.Post(() =>
            {
                if (_webView != null)
                    _webView.Source = new Uri(tempFile);
            });
        }

        private static string InjectBaseHref(string html, string htmlDir)
        {
            var baseUri = new Uri(Path.GetFullPath(htmlDir) + Path.DirectorySeparatorChar).AbsoluteUri;
            if (Regex.IsMatch(html, @"<base\s", RegexOptions.IgnoreCase))
                return html;

            if (Regex.IsMatch(html, @"<head\s*[^>]*>", RegexOptions.IgnoreCase))
            {
                return Regex.Replace(html,
                    @"(<head\s*[^>]*>)",
                    "$1<base href=\"" + baseUri + "\">",
                    RegexOptions.IgnoreCase);
            }

            if (Regex.IsMatch(html, @"<html\s*[^>]*>", RegexOptions.IgnoreCase))
            {
                return Regex.Replace(html,
                    @"(<html\s*[^>]*>)",
                    "$1<head><base href=\"" + baseUri + "\"></head>",
                    RegexOptions.IgnoreCase);
            }

            return "<head><base href=\"" + baseUri + "\"></head>" + html;
        }

        public void LoadHtml(string html)
        {
            html = InjectScrollbarCss(html);

            if (!_isInitialized || _webView == null)
            {
                _pendingHtml = html;
                _pendingUri = null;
                _pendingHtmlPath = null;
                return;
            }
            LoadHtmlInternal(html);
        }

        private void LoadHtmlInternal(string html)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_webView != null)
                    _webView.NavigateToString(html);
            });
        }

        public void LoadUrl(string url)
        {
            if (!_isInitialized || _webView == null)
            {
                _pendingUri = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u : null;
                _pendingHtml = null;
                _pendingHtmlPath = null;
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (_webView != null)
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        _webView.Source = uri;
                    else
                        _webView.NavigateToString($"<html><body>无效地址: {url}</body></html>");
                }
            });
        }

        public void GetScrollInfo(Action<double, double, double> callback) { }

        public void ScrollTo(double position)
        {
            if (_webView == null) return;
            var js = $"window.scrollTo(0, {position});";
            Dispatcher.UIThread.Post(() =>
            {
                if (_webView != null)
                    _ = _webView.InvokeScript(js);
            });
        }

        private const string ScrollbarCss =
            "<style id=\"__te_hide_scrollbar\">" +
            "html,body{overflow-y:auto!important;overflow-x:hidden!important;height:100%;-webkit-overflow-scrolling:touch;scrollbar-width:none!important;-ms-overflow-style:none!important;}" +
            "html::-webkit-scrollbar,body::-webkit-scrollbar,::-webkit-scrollbar{display:none!important;width:0!important;height:0!important;}" +
            "</style>";

        private static string InjectScrollbarCss(string html)
        {
            if (html.Contains("</head>", StringComparison.OrdinalIgnoreCase))
                return html.Replace("</head>", ScrollbarCss + "</head>", StringComparison.OrdinalIgnoreCase);
            if (html.Contains("<head>", StringComparison.OrdinalIgnoreCase))
                return html.Replace("<head>", "<head>" + ScrollbarCss, StringComparison.OrdinalIgnoreCase);
            return "<head>" + ScrollbarCss + "</head>" + html;
        }

        public NativeWebView? GetWebView() => _webView;

        public void SetOffscreenMode(bool enabled)
        {
            if (_webView == null) return;
            if (enabled)
            {
                _webView.IsVisible = false;
                _webView.IsHitTestVisible = false;
                Opacity = 0;
            }
            else
            {
                _webView.IsVisible = true;
                _webView.IsHitTestVisible = true;
                Opacity = 1;
            }
        }
    }

    public static class BrowserFactory
    {
        public static IHtmlBrowser CreateBrowser() => new BrowserControl();
        public static void Shutdown() { }
    }
}