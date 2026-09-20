using System.ComponentModel;
using System.Runtime.InteropServices;
using Bridge.Services.Infrastructure;

namespace Bridge.Services.Windows;

public sealed class TrayIconService : ITrayIconService
{
    private const uint IconId = 1;
    private const uint MenuTranslateSelection = 1001;
    private const uint MenuTranslateResponse = 1002;
    private const uint MenuSettings = 1003;
    private const uint MenuSignOut = 1004;
    private const uint MenuExit = 1005;
    private readonly NativeWindowService _nativeWindow;
    private readonly IDiagnosticLogger _logger;
    private NativeMethods.NotifyIconData _iconData;
    private nint _iconHandle;
    private string? _username;
    private bool _initialized;

    public nint LastInteractionSourceWindow { get; private set; }

    public TrayIconService(NativeWindowService nativeWindow, IDiagnosticLogger logger)
    {
        _nativeWindow = nativeWindow;
        _logger = logger;
        _nativeWindow.MessageReceived += OnMessageReceived;
    }

    public event EventHandler? TranslateSelectionRequested;
    public event EventHandler? TranslateResponseRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? SignOutRequested;
    public event EventHandler? ExitRequested;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _iconHandle = NativeMethods.LoadImageW(
            0,
            AppBranding.IconPath,
            NativeMethods.ImageIcon,
            32,
            32,
            NativeMethods.LrLoadFromFile);
        if (_iconHandle == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to load the Bridge icon.");
        }

        _iconData = CreateIconData(_iconHandle);
        if (!NativeMethods.Shell_NotifyIconW(NativeMethods.NimAdd, ref _iconData))
        {
            NativeMethods.DestroyIcon(_iconHandle);
            _iconHandle = 0;
            throw new InvalidOperationException("Unable to create the system tray icon.");
        }

        _initialized = true;
    }

    public void UpdateAccount(string? username) => _username = username;

    public void ShowNotification(string title, string message, bool warning = false)
    {
        if (!_initialized)
        {
            return;
        }

        var notification = _iconData;
        notification.Flags = NativeMethods.NifInfo;
        notification.InfoTitle = Limit(title, 63);
        notification.Info = Limit(message, 255);
        notification.InfoFlags = warning ? NativeMethods.NiifWarning : NativeMethods.NiifInfo;
        if (!NativeMethods.Shell_NotifyIconW(NativeMethods.NimModify, ref notification))
        {
            _logger.Error("System tray notification failed.");
        }
    }

    public void Dispose()
    {
        _nativeWindow.MessageReceived -= OnMessageReceived;
        if (_initialized)
        {
            NativeMethods.Shell_NotifyIconW(NativeMethods.NimDelete, ref _iconData);
            _initialized = false;
        }

        if (_iconHandle != 0)
        {
            NativeMethods.DestroyIcon(_iconHandle);
            _iconHandle = 0;
        }
    }

    private NativeMethods.NotifyIconData CreateIconData(nint iconHandle) => new()
    {
        Size = (uint)Marshal.SizeOf<NativeMethods.NotifyIconData>(),
        Window = _nativeWindow.Handle,
        Id = IconId,
        Flags = NativeMethods.NifMessage | NativeMethods.NifIcon | NativeMethods.NifTip,
        CallbackMessage = NativeMethods.WmAppTrayIcon,
        Icon = iconHandle,
        Tip = "Bridge",
        Info = string.Empty,
        InfoTitle = string.Empty
    };

    private void OnMessageReceived(object? sender, NativeWindowMessageEventArgs args)
    {
        if (args.Message != NativeMethods.WmAppTrayIcon)
        {
            return;
        }

        var mouseMessage = unchecked((uint)args.LParam.ToInt64());
        if (mouseMessage is NativeMethods.WmRButtonUp or NativeMethods.WmContextMenu)
        {
            ShowContextMenu();
            args.Handled = true;
        }
        else if (mouseMessage == NativeMethods.WmLButtonDoubleClick)
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
            args.Handled = true;
        }
    }

    private void ShowContextMenu()
    {
        LastInteractionSourceWindow = NativeMethods.GetForegroundWindow();
        var menu = NativeMethods.CreatePopupMenu();
        if (menu == 0)
        {
            return;
        }

        try
        {
            AppendDisabled(menu, "Bridge");
            NativeMethods.AppendMenuW(menu, NativeMethods.MfString | NativeMethods.MfChecked, 0, "Translation active");
            NativeMethods.AppendMenuW(menu, NativeMethods.MfString, MenuTranslateSelection, "Translate selection    Ctrl+Shift+T");
            NativeMethods.AppendMenuW(menu, NativeMethods.MfString, MenuTranslateResponse, "Translate response    Ctrl+Shift+Enter");
            NativeMethods.AppendMenuW(menu, NativeMethods.MfSeparator, 0, null);
            AppendDisabled(menu, "Microsoft 365 Copilot");
            AppendDisabled(menu, _username is null ? "Not connected" : $"Connected as: {_username}");
            NativeMethods.AppendMenuW(menu, NativeMethods.MfSeparator, 0, null);
            NativeMethods.AppendMenuW(menu, NativeMethods.MfString, MenuSettings, "Settings");
            NativeMethods.AppendMenuW(
                menu,
                _username is null ? NativeMethods.MfString | NativeMethods.MfDisabled | NativeMethods.MfGray : NativeMethods.MfString,
                MenuSignOut,
                "Sign out");
            NativeMethods.AppendMenuW(menu, NativeMethods.MfString, MenuExit, "Exit");

            NativeMethods.GetCursorPos(out var point);
            NativeMethods.SetForegroundWindow(_nativeWindow.Handle);
            var command = NativeMethods.TrackPopupMenuEx(
                menu,
                NativeMethods.TpmRightButton | NativeMethods.TpmReturnCommand,
                point.X,
                point.Y,
                _nativeWindow.Handle,
                0);

            DispatchMenuCommand(command);
        }
        finally
        {
            NativeMethods.DestroyMenu(menu);
        }
    }

    private static void AppendDisabled(nint menu, string text) =>
        NativeMethods.AppendMenuW(
            menu,
            NativeMethods.MfString | NativeMethods.MfDisabled | NativeMethods.MfGray,
            0,
            text);

    private void DispatchMenuCommand(uint command)
    {
        switch (command)
        {
            case MenuTranslateSelection:
                TranslateSelectionRequested?.Invoke(this, EventArgs.Empty);
                break;
            case MenuTranslateResponse:
                TranslateResponseRequested?.Invoke(this, EventArgs.Empty);
                break;
            case MenuSettings:
                SettingsRequested?.Invoke(this, EventArgs.Empty);
                break;
            case MenuSignOut:
                SignOutRequested?.Invoke(this, EventArgs.Empty);
                break;
            case MenuExit:
                ExitRequested?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private static string Limit(string value, int length) =>
        value.Length <= length ? value : value[..length];
}
