using System.Windows;
using System.Windows.Threading;
using ARIS.UI.Bootstrap;
using ARIS.UI.Views;

namespace ARIS.UI;

/// <summary>
/// Application entry point with global exception handling.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Handle unhandled exceptions on the UI thread
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        // Handle unhandled exceptions on background threads
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        // Handle unhandled exceptions in async methods
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        ShowErrorAndShutdown(e.Exception);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            ShowErrorAndShutdown(ex);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        ShowErrorAndShutdown(e.Exception);
    }

    private void ShowErrorAndShutdown(Exception exception)
    {
        try
        {
            // Hide main window if visible
            MainWindow?.Hide();

            // Show error dialog
            var errorWindow = exception is BootstrapException bootstrapEx
                ? new ErrorWindow(bootstrapEx)
                : new ErrorWindow(exception);

            errorWindow.ShowDialog();
        }
        catch
        {
            // Last resort - show message box
            MessageBox.Show(
                $"A fatal error occurred:\n\n{exception.Message}",
                "ARIS - Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            Shutdown(1);
        }
    }
}
