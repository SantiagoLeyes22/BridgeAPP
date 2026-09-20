using Microsoft.UI.Xaml;

namespace Bridge.Services.Windows;

public interface IWindowHandleProvider
{
    nint GetMainWindowHandle();

    void SetMainWindow(Window window);
}
