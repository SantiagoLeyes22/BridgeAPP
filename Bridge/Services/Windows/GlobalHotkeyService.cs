using System.Runtime.InteropServices;
using Bridge.Services.Infrastructure;

namespace Bridge.Services.Windows;

public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int TranslateSelectionId = 0x4A01;
    private const int TranslateResponseId = 0x4A02;
    private const uint VirtualKeyT = 0x54;
    private const uint VirtualKeyEnter = 0x0D;
    private readonly NativeWindowService _nativeWindow;
    private readonly IDiagnosticLogger _logger;
    private bool _selectionRegistered;
    private bool _responseRegistered;

    public GlobalHotkeyService(NativeWindowService nativeWindow, IDiagnosticLogger logger)
    {
        _nativeWindow = nativeWindow;
        _logger = logger;
        _nativeWindow.MessageReceived += OnMessageReceived;
    }

    public event EventHandler<TranslationHotkey>? Triggered;

    public HotkeyRegistrationResult Register()
    {
        var modifiers = NativeMethods.ModControl | NativeMethods.ModShift | NativeMethods.ModNoRepeat;
        _selectionRegistered = NativeMethods.RegisterHotKey(
            _nativeWindow.Handle,
            TranslateSelectionId,
            modifiers,
            VirtualKeyT);
        _responseRegistered = NativeMethods.RegisterHotKey(
            _nativeWindow.Handle,
            TranslateResponseId,
            modifiers,
            VirtualKeyEnter);

        if (!_selectionRegistered || !_responseRegistered)
        {
            _logger.Error($"Global hotkey registration failed. Win32={Marshal.GetLastWin32Error()}.");
        }

        return new HotkeyRegistrationResult(_selectionRegistered, _responseRegistered);
    }

    public void Dispose()
    {
        _nativeWindow.MessageReceived -= OnMessageReceived;
        if (_selectionRegistered)
        {
            NativeMethods.UnregisterHotKey(_nativeWindow.Handle, TranslateSelectionId);
        }

        if (_responseRegistered)
        {
            NativeMethods.UnregisterHotKey(_nativeWindow.Handle, TranslateResponseId);
        }
    }

    private void OnMessageReceived(object? sender, NativeWindowMessageEventArgs args)
    {
        if (args.Message != NativeMethods.WmHotkey)
        {
            return;
        }

        args.Handled = true;
        var id = unchecked((int)args.WParam);
        if (id == TranslateSelectionId)
        {
            Triggered?.Invoke(this, TranslationHotkey.TranslateSelection);
        }
        else if (id == TranslateResponseId)
        {
            Triggered?.Invoke(this, TranslationHotkey.TranslateResponse);
        }
    }
}
