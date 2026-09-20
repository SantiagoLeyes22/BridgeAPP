namespace Bridge.Services.Windows;

public interface IInputSimulationService
{
    Task<bool> WaitForShortcutReleaseAsync(CancellationToken cancellationToken);

    bool SendCopy();

    bool SendPaste();
}
