using System.Diagnostics;

namespace Bridge.Services.Infrastructure;

public sealed class DiagnosticLogger : IDiagnosticLogger
{
    private const long MaximumLogBytes = 1_000_000;
    private readonly object _gate = new();
    private readonly string _logPath = Path.Combine(
        AppBranding.DataDirectory,
        "Logs",
        "diagnostic.log");

    public void Info(string message)
    {
        Trace.TraceInformation(message);
        Append("INFO", message, null);
    }

    public void Error(string message, Exception? exception = null)
    {
        // Never include selected or translated content in diagnostic messages.
        Trace.TraceError(exception is null ? message : $"{message} ({exception.GetType().Name})");
        Append("ERROR", message, exception);
    }

    private void Append(string level, string message, Exception? exception)
    {
        try
        {
            lock (_gate)
            {
                var directory = Path.GetDirectoryName(_logPath)!;
                Directory.CreateDirectory(directory);
                if (File.Exists(_logPath) && new FileInfo(_logPath).Length > MaximumLogBytes)
                {
                    File.Move(_logPath, _logPath + ".previous", overwrite: true);
                }

                var exceptionDetails = exception is null
                    ? string.Empty
                    : $" Exception={exception.GetType().Name}; HResult=0x{exception.HResult:X8}; Inner={exception.InnerException?.GetType().Name ?? "none"}.";
                File.AppendAllText(
                    _logPath,
                    $"{DateTimeOffset.Now:O} [{level}] {message}{exceptionDetails}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never interrupt the application workflow.
        }
    }
}
