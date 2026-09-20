using Bridge.Models;
using Bridge.Services.Infrastructure;
using Bridge.Services.Settings;
using Bridge.Services.Windows;
using Bridge.UI.Overlay;

namespace Bridge.Services.Translation;

public sealed class TranslationCoordinator : IDisposable
{
    public const int MaximumInputCharacters = 8_000;
    public const string DefaultResponseLanguageCode = "en";
    private readonly IClipboardService _clipboard;
    private readonly ITranslationProvider _provider;
    private readonly OllamaTranslationProvider _ollamaProvider;
    private readonly ITranslationEngineService _engineService;
    private readonly TranslationOverlay _overlay;
    private readonly ITrayIconService _tray;
    private readonly IDiagnosticLogger _logger;
    private readonly ISettingsService _settingsService;
    private readonly SemaphoreSlim _translationGate = new(1, 1);
    private CancellationTokenSource? _currentOperation;
    private SelectedTextCapture? _currentCapture;
    private TranslationResult? _currentResult;
    private TranslationStyleDefinition _currentStyle = TranslationStyleDefinition.Balanced;

    public TranslationCoordinator(
        IClipboardService clipboard,
        ITranslationProvider provider,
        OllamaTranslationProvider ollamaProvider,
        ITranslationEngineService engineService,
        TranslationOverlay overlay,
        ITrayIconService tray,
        ISettingsService settingsService,
        IDiagnosticLogger logger)
    {
        _clipboard = clipboard;
        _provider = provider;
        _ollamaProvider = ollamaProvider;
        _engineService = engineService;
        _overlay = overlay;
        _tray = tray;
        _settingsService = settingsService;
        _logger = logger;
        _overlay.CopyRequested += OnCopyRequested;
        _overlay.ReplaceRequested += OnReplaceRequested;
        _overlay.RetranslateRequested += OnRetranslateRequested;
    }

    public Task TranslateSelectionAsync(nint sourceWindow = 0) =>
        BeginCaptureAndTranslateAsync(TranslationHotkey.TranslateSelection, sourceWindow);

    public Task TranslateResponseAsync(nint sourceWindow = 0) =>
        BeginCaptureAndTranslateAsync(TranslationHotkey.TranslateResponse, sourceWindow);

    public void Dispose()
    {
        _currentOperation?.Cancel();
        _currentOperation?.Dispose();
        _translationGate.Dispose();
        _overlay.CopyRequested -= OnCopyRequested;
        _overlay.ReplaceRequested -= OnReplaceRequested;
        _overlay.RetranslateRequested -= OnRetranslateRequested;
    }

    private async Task BeginCaptureAndTranslateAsync(TranslationHotkey hotkey, nint sourceWindow)
    {
        if (!await _translationGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            var engineStatus = await _engineService.GetStatusAsync(
                _settingsService.Settings.TranslationEngineId,
                CancellationToken.None);
            if (!engineStatus.IsReady)
            {
                ShowError(engineStatus.Message);
                return;
            }

            _currentOperation?.Cancel();
            _currentOperation?.Dispose();
            _currentOperation = new CancellationTokenSource(
                TimeSpan.FromSeconds(_engineService.SelectedEngine.TimeoutSeconds));
            var cancellationToken = _currentOperation.Token;

            var capture = await _clipboard.CaptureSelectedTextAsync(sourceWindow, cancellationToken);
            if (capture is null)
            {
                ShowError("Select some text and try again.");
                return;
            }

            if (capture.Text.Length > MaximumInputCharacters)
            {
                ShowError($"The selection is too long. Select up to {MaximumInputCharacters:N0} characters.");
                return;
            }

            var target = hotkey == TranslationHotkey.TranslateSelection
                ? GetPrimaryLanguage()
                : LanguageDefinition.FindByCode(DefaultResponseLanguageCode)!;
            await TranslateCaptureAsync(capture, target, hotkey, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            ShowError("The translation request timed out. Try again.");
        }
        catch (CopilotServiceException exception)
        {
            _logger.Error(
                $"Copilot translation failed. Kind={exception.Kind}; HTTP={exception.StatusCode?.ToString() ?? "none"}.");
            ShowError(GetFriendlyMessage(exception.Kind));
        }
        catch (TranslationEngineException exception)
        {
            _logger.Error($"Local translation failed. Provider={_provider.ProviderName}.", exception);
            ShowError(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.Error("Translation workflow failed.", exception);
            ShowError("Unable to translate the selected text. Try again.");
        }
        finally
        {
            _translationGate.Release();
        }
    }

    private async Task TranslateCaptureAsync(
        SelectedTextCapture capture,
        LanguageDefinition target,
        TranslationHotkey hotkey,
        CancellationToken cancellationToken)
    {
        _currentCapture = capture;
        _currentResult = null;
        _currentStyle = TranslationStyleDefinition.Balanced;
        _overlay.ShowLoading(_provider.ProviderName);

        var result = hotkey == TranslationHotkey.TranslateSelection
            ? await _provider.TranslateToPrimaryLanguageAsync(
                capture.Text,
                target.DisplayName,
                _currentStyle,
                cancellationToken)
            : await _provider.TranslateAsync(
                capture.Text,
                target.DisplayName,
                _currentStyle,
                cancellationToken);

        _currentResult = result;
        _overlay.ShowResult(result, _currentStyle, _provider.SupportsTranslationStyles);
    }

    private async void OnCopyRequested(object? sender, EventArgs e)
    {
        if (_currentResult is null)
        {
            return;
        }

        try
        {
            await _clipboard.CopyTextAsync(_currentResult.TranslatedText, CancellationToken.None);
            _tray.ShowNotification("Bridge", "Translation copied to clipboard.");
        }
        catch (Exception exception)
        {
            _logger.Error("Copying translated text failed.", exception);
            ShowError("Unable to access the clipboard. Try again.");
        }
    }

    private async void OnRetranslateRequested(object? sender, RetranslationOptions options)
    {
        if (_currentCapture is null || !await _translationGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            _currentOperation?.Cancel();
            _currentOperation?.Dispose();
            _currentStyle = options.Style;

            TranslationResult result;
            if (!_provider.SupportsTranslationStyles &&
                options.Style.Id != TranslationStyleDefinition.BalancedId)
            {
                var styleEngine = await FindBestInstalledStyleEngineAsync();
                if (styleEngine is null)
                {
                    throw new TranslationEngineException(
                        "Natural and Professional styles need TranslateGemma. Download any TranslateGemma model from Settings, then try again.");
                }

                _currentOperation = new CancellationTokenSource(
                    TimeSpan.FromSeconds(styleEngine.TimeoutSeconds));
                _overlay.ShowLoading(GetEngineProviderName(styleEngine));
                result = await _ollamaProvider.TranslateWithEngineAsync(
                    styleEngine,
                    _currentCapture.Text,
                    options.TargetLanguage.DisplayName,
                    _currentStyle,
                    _currentOperation.Token);
            }
            else
            {
                _currentOperation = new CancellationTokenSource(
                    TimeSpan.FromSeconds(_engineService.SelectedEngine.TimeoutSeconds));
                _overlay.ShowLoading(_provider.ProviderName);
                result = await _provider.TranslateAsync(
                    _currentCapture.Text,
                    options.TargetLanguage.DisplayName,
                    _currentStyle,
                    _currentOperation.Token);
            }

            _currentResult = result;
            _overlay.ShowResult(result, _currentStyle, _provider.SupportsTranslationStyles);
        }
        catch (OperationCanceledException)
        {
            ShowError("The translation request timed out. Try again or select a lighter engine.");
        }
        catch (CopilotServiceException exception)
        {
            _logger.Error(
                $"Copilot retranslation failed. Kind={exception.Kind}; HTTP={exception.StatusCode?.ToString() ?? "none"}.");
            ShowError(GetFriendlyMessage(exception.Kind));
        }
        catch (TranslationEngineException exception)
        {
            _logger.Error($"Local retranslation failed. Provider={_provider.ProviderName}.", exception);
            ShowError(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.Error("Retranslation workflow failed.", exception);
            ShowError("Unable to translate the selected text. Try again.");
        }
        finally
        {
            _translationGate.Release();
        }
    }

    private async void OnReplaceRequested(object? sender, EventArgs e)
    {
        if (_currentCapture is null || _currentResult is null)
        {
            return;
        }

        try
        {
            _overlay.HideOverlay();
            var replaced = await _clipboard.ReplaceSelectionAsync(
                _currentCapture.SourceWindow,
                _currentResult.TranslatedText,
                CancellationToken.None);
            if (!replaced)
            {
                _tray.ShowNotification(
                    "Bridge",
                    "Replacement was not available. The translation is on the clipboard.",
                    warning: true);
            }
        }
        catch (Exception exception)
        {
            _logger.Error("Replacing selected text failed.", exception);
            await _clipboard.CopyTextAsync(_currentResult.TranslatedText, CancellationToken.None);
            _tray.ShowNotification(
                "Bridge",
                "Replacement failed. The translation is on the clipboard.",
                warning: true);
        }
    }

    private static string GetFriendlyMessage(CopilotErrorKind kind) => kind switch
    {
        CopilotErrorKind.LicenseUnavailable =>
            "Your account does not appear to have access to Microsoft 365 Copilot. Your organization may also need to approve Bridge.",
        CopilotErrorKind.AdminConsentRequired =>
            "Your organization needs to approve Bridge before Copilot can be used.",
        CopilotErrorKind.NetworkUnavailable => "Unable to reach Microsoft 365 Copilot.",
        CopilotErrorKind.SessionExpired => "Your Microsoft session has expired. Sign in again.",
        CopilotErrorKind.RateLimited => "Microsoft 365 Copilot is busy. Wait a moment and try again.",
        CopilotErrorKind.Timeout => "The translation request timed out. Try again.",
        CopilotErrorKind.ApiCompatibility =>
            "Microsoft 365 Copilot returned an unexpected response. The preview API may have changed.",
        CopilotErrorKind.ParseFailure => "Copilot returned no usable translation. Try again.",
        _ => "Unable to translate the selected text. Try again."
    };

    private LanguageDefinition GetPrimaryLanguage() =>
        LanguageDefinition.FindByCode(_settingsService.Settings.PrimaryLanguageCode) ?? LanguageDefinition.Spanish;

    private async Task<TranslationEngineDefinition?> FindBestInstalledStyleEngineAsync()
    {
        using var statusTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        return await _ollamaProvider.FindBestInstalledEngineAsync(statusTimeout.Token);
    }

    private static string GetEngineProviderName(TranslationEngineDefinition engine) => engine.Id switch
    {
        TranslationEngineDefinition.TranslateGemma27BId => "TranslateGemma 27B",
        TranslationEngineDefinition.TranslateGemma12BId => "TranslateGemma 12B",
        _ => "TranslateGemma 4B"
    };

    private void ShowError(string message)
    {
        _overlay.ShowError(_provider.ProviderName, message);
        if (_settingsService.Settings.ShowFailureNotifications)
        {
            _tray.ShowNotification("Translation failed", message, warning: true);
        }
    }
}
