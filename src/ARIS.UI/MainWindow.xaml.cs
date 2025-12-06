using System.IO;
using System.Windows;

namespace ARIS.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeWebView();
    }

    private async void InitializeWebView()
    {
        await webView.EnsureCoreWebView2Async();

        var frontendPath = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..",
            "frontend", "dist", "index.html"));

        if (File.Exists(frontendPath))
        {
            webView.Source = new Uri($"file:///{frontendPath.Replace('\\', '/')}");
        }
        else
        {
            webView.NavigateToString(
                "<html><body style='background:#1a1a1a;color:#fff;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;font-family:sans-serif'>" +
                "<div style='text-align:center'>" +
                "<h1>ARIS Frontend</h1>" +
                "<p style='color:#888'>Phase 0 - Frontend not built</p>" +
                $"<p style='color:#666;font-size:12px'>Expected path: {frontendPath}</p>" +
                "<p style='color:#666;font-size:12px'>Run: cd frontend && npm run build</p>" +
                "</div></body></html>");
        }
    }
}