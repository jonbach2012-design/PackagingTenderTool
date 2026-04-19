using System.Text;

namespace PackagingTenderTool.App;

internal static class AppExceptionReporter
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PackagingTenderTool",
        "app-errors.log");

    public static void Handle(Exception exception)
    {
        try
        {
            Log(exception);
        }
        catch
        {
            // Error reporting must never become another crash path.
        }

        try
        {
            MessageBox.Show(
                "PackagingTenderTool encountered an unexpected problem and recovered where possible.\r\n\r\n" +
                "Details were written to the local error log.",
                "Unexpected problem",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch
        {
            // The process may be shutting down or no UI thread may be available.
        }
    }

    public static void LogSilently(Exception exception)
    {
        try
        {
            Log(exception);
        }
        catch
        {
            // Logging failures should not affect normal UI recovery.
        }
    }

    private static void Log(Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        var builder = new StringBuilder()
            .AppendLine($"[{DateTimeOffset.Now:O}] {exception.GetType().FullName}")
            .AppendLine(exception.Message)
            .AppendLine(exception.StackTrace)
            .AppendLine();

        File.AppendAllText(LogPath, builder.ToString());
    }
}
