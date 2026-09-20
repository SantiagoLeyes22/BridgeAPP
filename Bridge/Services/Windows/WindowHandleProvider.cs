using Microsoft.UI.Xaml;

namespace Bridge.Services.Windows;

public sealed class WindowHandleProvider : IWindowHandleProvider
{
    private nint _mainWindowHandle;

    public nint GetMainWindowHandle() => _mainWindowHandle;

    public void SetMainWindow(Window window) =>
        _mainWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
}
