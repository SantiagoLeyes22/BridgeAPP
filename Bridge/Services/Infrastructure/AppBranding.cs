using System.Diagnostics;

namespace Bridge.Services.Infrastructure;

public static class AppBranding
{
    public const string Name = "Bridge";

    private const string LegacyDataDirectoryName = "TranslationAssistant";

    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Name);

    public static string IconPath { get; } = Path.Combine(
        AppContext.BaseDirectory,
        "Assets",
        "Bridge.ico");

    public static void MigrateLegacyDataDirectory()
    {
        var legacyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LegacyDataDirectoryName);

        if (Directory.Exists(DataDirectory) || !Directory.Exists(legacyDirectory))
        {
            return;
        }

        try
        {
            Directory.Move(legacyDirectory, DataDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"Could not migrate the legacy application data directory: {exception.GetType().Name}.");
        }
    }
}
