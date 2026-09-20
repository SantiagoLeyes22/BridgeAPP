namespace Bridge.Services.Infrastructure;

public interface IDiagnosticLogger
{
    void Info(string message);

    void Error(string message, Exception? exception = null);
}
