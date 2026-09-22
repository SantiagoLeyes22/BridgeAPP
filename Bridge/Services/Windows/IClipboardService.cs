using Bridge.Models;

namespace Bridge.Services.Windows;

public interface IClipboardService
{
    Task<SelectedTextCapture?> CaptureSelectedTextAsync(
        nint sourceWindow,
        CancellationToken cancellationToken,
        bool restoreSourceWindow = true);

    Task CopyTextAsync(string text, CancellationToken cancellationToken);

    Task<bool> ReplaceSelectionAsync(
        nint sourceWindow,
        string translatedText,
        CancellationToken cancellationToken);
}
