using System.Runtime.InteropServices;

namespace Bridge.Services.Windows;

public sealed class ForegroundWindowService : IForegroundWindowService
{
    public nint GetForegroundWindow() => NativeMethods.GetForegroundWindow();

    public bool RestoreForegroundWindow(nint window)
    {
        if (window == 0)
        {
            return false;
        }

        if (NativeMethods.IsIconic(window))
        {
            NativeMethods.ShowWindowAsync(window, NativeMethods.SwRestore);
        }

        return NativeMethods.SetForegroundWindow(window);
    }

    public (int X, int Y) GetOverlayPosition(int width, int height)
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            return (80, 80);
        }

        var monitor = NativeMethods.MonitorFromPoint(point, NativeMethods.MonitorDefaultToNearest);
        var info = new NativeMethods.MonitorInfo { Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (monitor == 0 || !NativeMethods.GetMonitorInfoW(monitor, ref info))
        {
            return (point.X + 16, point.Y + 16);
        }

        const int margin = 12;
        var x = Math.Clamp(point.X + 16, info.WorkArea.Left + margin, info.WorkArea.Right - width - margin);
        var y = Math.Clamp(point.Y + 20, info.WorkArea.Top + margin, info.WorkArea.Bottom - height - margin);
        return (x, y);
    }
}
