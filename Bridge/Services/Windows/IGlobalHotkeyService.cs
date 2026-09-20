namespace Bridge.Services.Windows;

public enum TranslationHotkey
{
    TranslateSelection,
    TranslateResponse
}

public sealed record HotkeyRegistrationResult(bool SelectionRegistered, bool ResponseRegistered)
{
    public bool AllRegistered => SelectionRegistered && ResponseRegistered;
}

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler<TranslationHotkey>? Triggered;

    HotkeyRegistrationResult Register();
}
