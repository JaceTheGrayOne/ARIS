using System.IO;
using System.Reflection;
using System.Windows;
using ARIS.UI.Bootstrap;
using ARIS.UI.Views;

namespace ARIS.UI;

/// <summary>
/// Main window that bootstraps the backend and hosts the WebView2 frontend.
/// </summary>
public partial class MainWindow : Window
{
    private BackendProcessManager? _backendManager;
    private string? _backendUrl;

    public MainWindow()
    {
        InitializeComponent();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await BootstrapAsync();
        }
        catch (BootstrapException ex)
        {
            ShowError(ex);
        }
        catch (Exception ex)
        {
            ShowError(new BootstrapException("UNEXPECTED_ERROR", ex.Message, ex, "Please check logs and try again."));
        }
    }

    private async Task BootstrapAsync()
    {
        // Step 1: Check if we're in development mode (no embedded payload)
        var assembly = Assembly.GetExecutingAssembly();
        var hasPayload = assembly.GetManifestResourceStream("ARIS.Payload") != null;

        if (!hasPayload)
        {
            // Development mode - load frontend from file system
            await InitializeDevelopmentModeAsync();
            return;
        }

        // Production mode - full bootstrap
        await InitializeProductionModeAsync();
    }

    private async Task InitializeDevelopmentModeAsync()
    {
        UpdateStatus("Development mode - loading frontend...");

        await webView.EnsureCoreWebView2Async();

        var frontendPath = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..",
            "frontend", "dist", "index.html"));

        if (File.Exists(frontendPath))
        {
            webView.Source = new Uri($"file:///{frontendPath.Replace('\\', '/')}");
            UpdateStatus("Backend: localhost:5000 (dev mode)");
        }
        else
        {
            webView.NavigateToString(
                "<html><body style='background:#1a1a1a;color:#fff;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;font-family:sans-serif'>" +
                "<div style='text-align:center'>" +
                "<h1>ARIS Development Mode</h1>" +
                "<p style='color:#888'>Frontend not built</p>" +
                $"<p style='color:#666;font-size:12px'>Expected path: {frontendPath}</p>" +
                "<p style='color:#666;font-size:12px'>Run: cd frontend && npm run build</p>" +
                "</div></body></html>");
            UpdateStatus("Frontend not built - run npm run build");
        }

        ShowWebView();
    }

    private async Task InitializeProductionModeAsync()
    {
        // Step 1: Extract payload
        UpdateStatus("Extracting application files...");
        var extractor = new PayloadExtractor();
        var hostingExePath = await extractor.ExtractAsync();

        // Step 2: Initialize WebView2 in parallel with backend startup
        var webViewTask = webView.EnsureCoreWebView2Async();

        // Step 3: Start backend and wait for URL
        UpdateStatus("Starting backend...");
        _backendManager = new BackendProcessManager();
        _backendUrl = await _backendManager.StartAndWaitForUrlAsync(hostingExePath);

        UpdateStatus($"Backend started at {_backendUrl}");

        // Step 4: Wait for backend readiness
        UpdateStatus("Waiting for backend to be ready...");
        using var poller = new ReadinessPoller();
        await poller.WaitForReadyAsync(_backendUrl);

        // Step 5: Ensure WebView2 is initialized
        await webViewTask;

        // Step 6: Navigate to backend
        UpdateStatus($"Connected to backend");
        webView.Source = new Uri(_backendUrl);

        ShowWebView();
    }

    private void UpdateStatus(string message)
    {
        Dispatcher.Invoke(() =>
        {
            LoadingStatus.Text = message;
            statusText.Text = message;
        });
    }

    private void ShowWebView()
    {
        Dispatcher.Invoke(() =>
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            webView.Visibility = Visibility.Visible;
        });
    }

    private void ShowError(BootstrapException ex)
    {
        Dispatcher.Invoke(() =>
        {
            // Hide this window and show error dialog
            Hide();

            var errorWindow = new ErrorWindow(ex);
            errorWindow.ShowDialog();

            // Shutdown after error dialog is closed
            Application.Current.Shutdown(1);
        });
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Stop the backend process when the window closes
        _backendManager?.Dispose();
    }
}
