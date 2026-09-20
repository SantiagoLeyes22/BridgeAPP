namespace Bridge.Services.Windows;

public interface IStartupService
{
    bool IsEnabled();

    void SetEnabled(bool enabled);
}
