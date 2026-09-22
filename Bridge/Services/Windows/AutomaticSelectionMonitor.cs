using System.Runtime.InteropServices;
using Bridge.Services.Infrastructure;

namespace Bridge.Services.Windows;

public sealed class AutomaticSelectionMonitor : IDisposable
{
    private const int MouseHookType = 14;
    private const int KeyboardHookType = 13;
    private const uint LeftButtonDown = 0x0201;
    private const uint LeftButtonUp = 0x0202;
    private const uint MouseMove = 0x0200;
    private const uint KeyDown = 0x0100;
    private const uint KeyUp = 0x0101;
    private const uint SystemKeyDown = 0x0104;
    private const uint SystemKeyUp = 0x0105;
    private const uint InjectedMouseEvent = 0x00000001;
    private const uint InjectedKeyboardEvent = 0x00000010;
    private readonly NativeMethods.HookProcedure _mouseProcedure;
    private readonly NativeMethods.HookProcedure _keyboardProcedure;
    private readonly SelectionGestureDetector _gestures = new();
    private readonly IDiagnosticLogger _logger;
    private nint _mouseHook;
    private nint _keyboardHook;

    public AutomaticSelectionMonitor(IDiagnosticLogger logger)
    {
        _logger = logger;
        _mouseProcedure = OnMouseEvent;
        _keyboardProcedure = OnKeyboardEvent;
    }

    public event EventHandler<nint>? SelectionCompleted;

    public void Start()
    {
        if (_mouseHook != 0 || _keyboardHook != 0)
        {
            return;
        }

        _gestures.Reset();
        var module = NativeMethods.GetModuleHandle(null);
        _mouseHook = NativeMethods.SetWindowsHookEx(MouseHookType, _mouseProcedure, module, 0);
        if (_mouseHook == 0)
        {
            _logger.Error($"Automatic mouse selection monitoring could not start. Win32={Marshal.GetLastWin32Error()}.");
            return;
        }

        _keyboardHook = NativeMethods.SetWindowsHookEx(KeyboardHookType, _keyboardProcedure, module, 0);
        if (_keyboardHook == 0)
        {
            _logger.Error($"Automatic keyboard selection monitoring could not start. Win32={Marshal.GetLastWin32Error()}.");
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = 0;
        }
    }

    public void Stop()
    {
        if (_mouseHook != 0)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = 0;
        }

        if (_keyboardHook != 0)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = 0;
        }

        _gestures.Reset();
    }

    public void Dispose() => Stop();

    private nint OnMouseEvent(int code, nuint message, nint parameter)
    {
        if (code >= 0 && _mouseHook != 0)
        {
            try
            {
                var data = Marshal.PtrToStructure<NativeMethods.MouseHookData>(parameter);
                if ((data.Flags & InjectedMouseEvent) == 0)
                {
                    var window = NativeMethods.GetForegroundWindow();
                    switch ((uint)message)
                    {
                        case LeftButtonDown:
                            _gestures.MouseDown(data.Point.X, data.Point.Y);
                            break;
                        case MouseMove:
                            _gestures.MouseMove(data.Point.X, data.Point.Y);
                            break;
                        case LeftButtonUp:
                            if (_gestures.MouseUp(window, data.Point.X, data.Point.Y, data.Time))
                            {
                                RaiseCandidate(window);
                            }
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error("Automatic mouse selection monitoring failed.", exception);
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHook, code, message, parameter);
    }

    private nint OnKeyboardEvent(int code, nuint message, nint parameter)
    {
        if (code >= 0 && _keyboardHook != 0)
        {
            try
            {
                var data = Marshal.PtrToStructure<NativeMethods.KeyboardHookData>(parameter);
                if ((data.Flags & InjectedKeyboardEvent) == 0)
                {
                    if ((uint)message is KeyDown or SystemKeyDown)
                    {
                        _gestures.KeyDown(data.VirtualKey);
                    }
                    else if (((uint)message is KeyUp or SystemKeyUp) && _gestures.KeyUp(data.VirtualKey))
                    {
                        RaiseCandidate(NativeMethods.GetForegroundWindow());
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error("Automatic keyboard selection monitoring failed.", exception);
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, code, message, parameter);
    }

    private void RaiseCandidate(nint window)
    {
        if (window == 0 || NativeMethods.GetWindowThreadProcessId(window, out var processId) == 0 ||
            processId == Environment.ProcessId)
        {
            return;
        }

        SelectionCompleted?.Invoke(this, window);
    }
}