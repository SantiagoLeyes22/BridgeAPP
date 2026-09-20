namespace Bridge.Services.Windows;

public interface IForegroundWindowService
{
    nint GetForegroundWindow();

    bool RestoreForegroundWindow(nint window);

    (int X, int Y) GetOverlayPosition(int width, int height);
}
