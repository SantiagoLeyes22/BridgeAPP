using Bridge.Services.Authentication;
using Bridge.Services.Infrastructure;
using Bridge.Services.Translation;
using Bridge.Services.Windows;

namespace Bridge;

public sealed class ApplicationController : IDisposable
{
    private readonly ITrayIconService _tray;
    private readonly IGlobalHotkeyService _hotkeys;
    private readonly TranslationCoordinator _translationCoordinator;
    private readonly IAuthenticationService _authentication;
    private readonly IDiagnosticLogger _logger;
    private MainWindow? _mainWindow;

    public ApplicationController(
        ITrayIconService tray,
        IGlobalHotkeyService hotkeys,
        TranslationCoordinator translationCoordinator,
        IAuthenticationService authentication,
        IDiagnosticLogger logger)
    {
        _tray = tray;
        _hotkeys = hotkeys;
        _translationCoordinator = translationCoordinator;
        _authentication = authentication;
        _logger = logger;
    }

    public event EventHandler? ExitRequested;

    public async Task StartAsync(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _tray.Initialize();
        _tray.TranslateSelectionRequested += OnTranslateSelectionRequested;
        _tray.TranslateResponseRequested += OnTranslateResponseRequested;
        _tray.SettingsRequested += OnSettingsRequested;
        _tray.SignOutRequested += OnSignOutRequested;
        _tray.ExitRequested += OnExitRequested;
        _hotkeys.Triggered += OnHotkeyTriggered;
        _authentication.AuthenticationStateChanged += OnAuthenticationStateChanged;

        var registrations = _hotkeys.Register();
        if (!registrations.AllRegistered)
        {
            _tray.ShowNotification(
                "Shortcut conflict",
                "One or more translation shortcuts are already in use. Open Settings for details.",
                warning: true);
        }

        await _authentication.InitializeAsync();
        UpdateTrayAccount();
        _logger.Info("Bridge started.");
    }

    public void Dispose()
    {
        _tray.TranslateSelectionRequested -= OnTranslateSelectionRequested;
        _tray.TranslateResponseRequested -= OnTranslateResponseRequested;
        _tray.SettingsRequested -= OnSettingsRequested;
        _tray.SignOutRequested -= OnSignOutRequested;
        _tray.ExitRequested -= OnExitRequested;
        _hotkeys.Triggered -= OnHotkeyTriggered;
        _authentication.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }

    private void OnHotkeyTriggered(object? sender, TranslationHotkey hotkey)
    {
        if (hotkey == TranslationHotkey.TranslateSelection)
        {
            _ = _translationCoordinator.TranslateSelectionAsync();
        }
        else
        {
            _ = _translationCoordinator.TranslateResponseAsync();
        }
    }

    private void OnTranslateSelectionRequested(object? sender, EventArgs e) =>
        _ = _translationCoordinator.TranslateSelectionAsync(_tray.LastInteractionSourceWindow);

    private void OnTranslateResponseRequested(object? sender, EventArgs e) =>
        _ = _translationCoordinator.TranslateResponseAsync(_tray.LastInteractionSourceWindow);

    private void OnSettingsRequested(object? sender, EventArgs e) => _mainWindow?.ShowSettings();

    private async void OnSignOutRequested(object? sender, EventArgs e)
    {
        try
        {
            await _authentication.SignOutAsync();
            UpdateTrayAccount();
            _mainWindow?.RefreshAuthenticationState();
        }
        catch (Exception exception)
        {
            _logger.Error("Sign out failed.", exception);
        }
    }

    private void OnAuthenticationStateChanged(object? sender, EventArgs e)
    {
        UpdateTrayAccount();
        _mainWindow?.RefreshAuthenticationState();
    }

    private void UpdateTrayAccount() =>
        _tray.UpdateAccount(_authentication.GetCurrentAccount()?.Username);

    private void OnExitRequested(object? sender, EventArgs e) => ExitRequested?.Invoke(this, EventArgs.Empty);
}
