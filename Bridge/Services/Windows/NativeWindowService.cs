using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Bridge.Services.Windows;

public sealed class NativeWindowMessageEventArgs(uint message, nuint wParam, nint lParam) : EventArgs
{
    public uint Message { get; } = message;

    public nuint WParam { get; } = wParam;

    public nint LParam { get; } = lParam;

    public bool Handled { get; set; }

    public nint Result { get; set; }
}

public sealed class NativeWindowService : IDisposable
{
    private readonly NativeMethods.WindowProcedure _windowProcedure;
    private readonly string _className = $"Bridge.NativeWindow.{Guid.NewGuid():N}";
    private readonly nint _instance;
    private bool _disposed;

    public NativeWindowService()
    {
        _instance = NativeMethods.GetModuleHandle(null);
        _windowProcedure = WindowProcedure;

        var windowClass = new NativeMethods.WindowClass
        {
            WindowProcedure = _windowProcedure,
            Instance = _instance,
            ClassName = _className
        };

        if (NativeMethods.RegisterClassW(ref windowClass) == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to register the native message window.");
        }

        Handle = NativeMethods.CreateWindowExW(
            0,
            _className,
            "Bridge",
            0,
            0,
            0,
            0,
            0,
            NativeMethods.HwndMessage,
            0,
            _instance,
            0);

        if (Handle == 0)
        {
            NativeMethods.UnregisterClassW(_className, _instance);
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create the native message window.");
        }
    }

    public nint Handle { get; }

    public event EventHandler<NativeWindowMessageEventArgs>? MessageReceived;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        NativeMethods.DestroyWindow(Handle);
        NativeMethods.UnregisterClassW(_className, _instance);
    }

    private nint WindowProcedure(nint window, uint message, nuint wParam, nint lParam)
    {
        var args = new NativeWindowMessageEventArgs(message, wParam, lParam);
        MessageReceived?.Invoke(this, args);
        return args.Handled ? args.Result : NativeMethods.DefWindowProcW(window, message, wParam, lParam);
    }
}
