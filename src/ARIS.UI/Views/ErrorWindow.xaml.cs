using System.Diagnostics;
using System.IO;
using System.Windows;
using ARIS.UI.Bootstrap;

namespace ARIS.UI.Views;

/// <summary>
/// Error window for displaying bootstrap failures to the user.
/// </summary>
public partial class ErrorWindow : Window
{
    public ErrorWindow()
    {
        InitializeComponent();
    }

    public ErrorWindow(BootstrapException exception) : this()
    {
        TitleText.Text = GetTitleForError(exception);
        ErrorCodeText.Text = exception.ErrorCode;
        MessageText.Text = exception.Message;

        if (!string.IsNullOrEmpty(exception.RemediationHint))
        {
            RemediationText.Text = exception.RemediationHint;
            RemediationBorder.Visibility = Visibility.Visible;
        }
        else
        {
            RemediationBorder.Visibility = Visibility.Collapsed;
        }
    }

    public ErrorWindow(Exception exception) : this()
    {
        TitleText.Text = "Unexpected Error";
        ErrorCodeText.Text = "UNEXPECTED_ERROR";
        MessageText.Text = exception.Message;

        if (exception.InnerException != null)
        {
            MessageText.Text += $"\n\nDetails: {exception.InnerException.Message}";
        }

        RemediationText.Text = "Please check the logs for more details and try restarting the application.";
        RemediationBorder.Visibility = Visibility.Visible;
    }

    private static string GetTitleForError(BootstrapException exception)
    {
        return exception switch
        {
            PayloadNotFoundException => "Installation Error",
            PayloadExtractionException => "Extraction Error",
            BackendStartException => "Backend Failed to Start",
            BackendUrlTimeoutException => "Backend Timeout",
            BackendReadinessTimeoutException => "Backend Not Ready",
            _ => "Startup Error"
        };
    }

    private void OpenLogsButton_Click(object sender, RoutedEventArgs e)
    {
        var logsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ARIS",
            "logs");

        try
        {
            if (Directory.Exists(logsPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logsPath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show(
                    $"Logs directory not found:\n{logsPath}",
                    "Logs Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open logs directory:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown(1);
    }
}
