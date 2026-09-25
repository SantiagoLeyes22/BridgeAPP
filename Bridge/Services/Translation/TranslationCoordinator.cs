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
    private readonly IForegroundWindowService _foregroundWindow;
    private readonly AutomaticSelectionMonitor _selectionMonitor;
    private readonly ITrayIconService _tray;
    private readonly IDiagnosticLogger _logger;
    private readonly ISettingsService _settingsService;
    private readonly SemaphoreSlim _translationGate = new(1, 1);
    private CancellationTokenSource? _currentOperation;
    private SelectedTextCapture? _currentCapture;
    private TranslationResult? _currentResult;
    private TranslationStyleDefinition _currentStyle = TranslationStyleDefinition.Balanced;
    private CancellationTokenSource? _selectionDebounce;
    private TranslationHotkey? _openMode;
    private bool _wasClosedDuringOperation;

    public TranslationCoordinator(
        IClipboardService clipboard,
        ITranslationProvider provider,
        OllamaTranslationProvider ollamaProvider,
        ITranslationEngineService engineService,
        TranslationOverlay overlay,
        IForegroundWindowService foregroundWindow,
        AutomaticSelectionMonitor selectionMonitor,
        ITrayIconService tray,
        ISettingsService settingsService,
        IDiagnosticLogger logger)
    {
        _clipboard = clipboard;
        _provider = provider;
        _ollamaProvider = ollamaProvider;
        _engineService = engineService;
        _overlay = overlay;
        _foregroundWindow = foregroundWindow;
        _selectionMonitor = selectionMonitor;
        _tray = tray;
        _settingsService = settingsService;
        _logger = logger;
        _overlay.CopyRequested += OnCopyRequested;
        _overlay.ReplaceRequested += OnReplaceRequested;
        _overlay.RetranslateRequested += OnRetranslateRequested;
        _overlay.OpenStateChanged += OnOverlayOpenStateChanged;
        _selectionMonitor.SelectionCompleted += OnSelectionCompleted;
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
        _overlay.OpenStateChanged -= OnOverlayOpenStateChanged;
        _selectionMonitor.SelectionCompleted -= OnSelectionCompleted;
        _selectionDebounce?.Cancel();
        _selectionMonitor.Stop();
    }

    private void OnOverlayOpenStateChanged(object? sender, bool isOpen)
    {
        if (isOpen)
        {
            _selectionMonitor.Start();
            return;
        }

        _selectionMonitor.Stop();
        var pending = _selectionDebounce;
        _selectionDebounce = null;
        pending?.Cancel();
        _openMode = null;
        _wasClosedDuringOperation = true;
        _currentOperation?.Cancel();
    }

    private void OnSelectionCompleted(object? sender, nint sourceWindow)
    {
        if (!_overlay.IsOpen || _openMode is not TranslationHotkey mode)
        {
            return;
        }

        _selectionDebounce?.Cancel();
        _overlay.SetReplacementEnabled(false);
        var pending = new CancellationTokenSource();
        _selectionDebounce = pending;
        _ = TranslateAutomaticSelectionAfterDelayAsync(sourceWindow, mode, pending);
    }

    private async Task TranslateAutomaticSelectionAfterDelayAsync(
        nint sourceWindow,
        TranslationHotkey mode,
        CancellationTokenSource pending)
    {
        try
        {
            await Task.Delay(300, pending.Token);
            if (!_overlay.IsOpen || _openMode != mode ||
                _foregroundWindow.GetForegroundWindow() != sourceWindow)
            {
                return;
            }

            await BeginCaptureAndTranslateAsync(mode, sourceWindow, automatic: true, pending.Token);
        }
        catch (OperationCanceledException)
        {
            // A newer selection or a closed overlay superseded this one.
        }
        finally
        {
            if (ReferenceEquals(_selectionDebounce, pending))
            {
                _selectionDebounce = null;
            }

            pending.Dispose();
        }
    }

    private async Task BeginCaptureAndTranslateAsync(
        TranslationHotkey hotkey,
        nint sourceWindow,
        bool automatic = false,
        CancellationToken selectionToken = default)
    {
        if (automatic && (!_overlay.IsOpen || _openMode != hotkey))
        {
            return;
        }

        if (automatic)
        {
            try
            {
                await _translationGate.WaitAsync(selectionToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
        else if (!await _translationGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            selectionToken.ThrowIfCancellationRequested();
            if (!automatic)
            {
                _openMode = hotkey;
                _wasClosedDuringOperation = false;
            }
            else if (!_overlay.IsOpen || _foregroundWindow.GetForegroundWindow() != sourceWindow)
            {
                return;
            }

            var engineStatus = await _engineService.GetStatusAsync(
                _settingsService.Settings.TranslationEngineId,
                automatic ? selectionToken : CancellationToken.None);
            if (!engineStatus.IsReady)
            {
                ShowError(engineStatus.Message, automatic);
                return;
            }

            _currentOperation?.Cancel();
            _currentOperation?.Dispose();
            _currentOperation = automatic
                ? CancellationTokenSource.CreateLinkedTokenSource(selectionToken)
                : new CancellationTokenSource();
            _currentOperation.CancelAfter(TimeSpan.FromSeconds(_engineService.SelectedEngine.TimeoutSeconds));
            var cancellationToken = _currentOperation.Token;

            if (automatic && (!_overlay.IsOpen || _foregroundWindow.GetForegroundWindow() != sourceWindow))
            {
                return;
            }

            var capture = await _clipboard.CaptureSelectedTextAsync(
                sourceWindow,
                cancellationToken,
                restoreSourceWindow: !automatic);
            if (capture is null)
            {
                if (!automatic || _currentResult is null)
                {
                    ShowError("Select some text and try again.", automatic);
                }

                return;
            }

            if (capture.Text.Length > MaximumInputCharacters)
            {
                ShowError($"The selection is too long. Select up to {MaximumInputCharacters:N0} characters.", automatic);
                return;
            }

            if (automatic && _currentCapture is not null &&
                capture.SourceWindow == _currentCapture.SourceWindow &&
                capture.Text == _currentCapture.Text)
            {
                _overlay.SetReplacementEnabled(true);
                return;
            }

            var target = automatic
                ? LanguageDefinition.FindByCode(_currentResult?.TargetLanguageCode) ??
                  TranslationTargetLanguagePreference.Resolve(_settingsService.Settings, hotkey)
                : TranslationTargetLanguagePreference.Resolve(_settingsService.Settings, hotkey);
            await TranslateCaptureAsync(capture, target, hotkey, cancellationToken, automatic);
        }
        catch (OperationCanceledException)
        {
            if (!automatic || !selectionToken.IsCancellationRequested)
            {
                ShowError("The translation request timed out. Try again.", automatic);
            }
        }
        catch (CopilotServiceException exception)
        {
            _logger.Error(
                $"Copilot translation failed. Kind={exception.Kind}; HTTP={exception.StatusCode?.ToString() ?? "none"}.");
            ShowError(GetFriendlyMessage(exception.Kind), automatic);
        }
        catch (TranslationEngineException exception)
        {
            _logger.Error($"Local translation failed. Provider={_provider.ProviderName}.", exception);
            ShowError(exception.Message, automatic);
        }
        catch (Exception exception)
        {
            _logger.Error("Translation workflow failed.", exception);
            ShowError("Unable to translate the selected text. Try again.", automatic);
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
        CancellationToken cancellationToken,
        bool automatic)
    {
        _currentCapture = capture;
        _currentResult = null;
        if (!automatic)
        {
            _currentStyle = TranslationStyleDefinition.Balanced;
        }

        _overlay.ShowLoading(_provider.ProviderName, activateWindow: !automatic);

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

        cancellationToken.ThrowIfCancellationRequested();
        if (_wasClosedDuringOperation || !_overlay.IsOpen)
        {
            return;
        }

        _currentResult = result;
        _overlay.ShowResult(
            result,
            _currentStyle,
            _provider.SupportsTranslationStyles,
            activateWindow: !automatic && _selectionDebounce is null);
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
        if (_currentCapture is null)
        {
            return;
        }

        if (_openMode == TranslationHotkey.TranslateResponse &&
            _currentResult is not null &&
            !string.Equals(options.TargetLanguage.Code, _currentResult.TargetLanguageCode, StringComparison.OrdinalIgnoreCase))
        {
            _settingsService.Settings.LastTargetLanguageCode = options.TargetLanguage.Code;
            try
            {
                await _settingsService.SaveAsync();
            }
            catch (Exception exception)
            {
                _logger.Error("Saving the preferred translation language failed.", exception);
            }
        }

        if (!_overlay.IsOpen || !await _translationGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            _wasClosedDuringOperation = false;
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

            if (_wasClosedDuringOperation || !_overlay.IsOpen)
            {
                return;
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
        var capture = _currentCapture;
        var result = _currentResult;
        if (capture is null || result is null || !_overlay.IsOpen)
        {
            return;
        }

        try
        {
            var selectedNow = await _clipboard.CaptureSelectedTextAsync(
                capture.SourceWindow,
                CancellationToken.None);
            if (!_overlay.IsOpen)
            {
                return;
            }

            if (selectedNow is null || selectedNow.Text != capture.Text)
            {
                _overlay.SetReplacementEnabled(false);
                _tray.ShowNotification(
                    "Bridge",
                    "The selected text changed. Select it again to update the translation before replacing.",
                    warning: true);
                return;
            }

            _overlay.HideOverlay();
            var replaced = await _clipboard.ReplaceSelectionAsync(
                capture.SourceWindow,
                result.TranslatedText,
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
            try
            {
                await _clipboard.CopyTextAsync(result.TranslatedText, CancellationToken.None);
            }
            catch (Exception copyException)
            {
                _logger.Error("Copying translated text after replacement failed.", copyException);
            }

            _tray.ShowNotification(
                "Bridge",
                "Replacement failed. The translation is on the clipboard if it could be copied.",
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

    private void ShowError(string message, bool automatic = false)
    {
        if (_wasClosedDuringOperation || (automatic && !_overlay.IsOpen))
        {
            return;
        }

        _overlay.ShowError(_provider.ProviderName, message, activateWindow: !automatic);
        if (_settingsService.Settings.ShowFailureNotifications)
        {
            _tray.ShowNotification("Translation failed", message, warning: true);
        }
    }
}
