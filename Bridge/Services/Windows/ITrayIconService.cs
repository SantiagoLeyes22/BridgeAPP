namespace Bridge.Services.Windows;

public interface ITrayIconService : IDisposable
{
    nint LastInteractionSourceWindow { get; }

    event EventHandler? TranslateSelectionRequested;

    event EventHandler? TranslateResponseRequested;

    event EventHandler? SettingsRequested;

    event EventHandler? SignOutRequested;

    event EventHandler? ExitRequested;

    void Initialize();

    void UpdateAccount(string? username);

    void ShowNotification(string title, string message, bool warning = false);
}
