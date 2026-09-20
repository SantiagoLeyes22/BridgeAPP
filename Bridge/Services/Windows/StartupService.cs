using Microsoft.Win32;

namespace Bridge.Services.Windows;

public sealed class StartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Bridge";
    private const string LegacyValueName = "TranslationAssistant";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string value &&
               string.Equals(value, GetCommand(), StringComparison.OrdinalIgnoreCase) ||
               key?.GetValue(LegacyValueName) is string;
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                        ?? throw new InvalidOperationException("Unable to open the Windows startup registry key.");
        if (enabled)
        {
            key.SetValue(ValueName, GetCommand(), RegistryValueKind.String);
            key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
        }
    }

    private static string GetCommand() => $"\"{Environment.ProcessPath}\"";
}
