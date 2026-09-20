namespace Bridge.Models;

public sealed class AppSettings
{
    public string ConfigurationLanguageCode { get; set; } = GetDefaultConfigurationLanguage();

    public string PrimaryLanguageCode { get; set; } = "es";

    public bool StartWithWindows { get; set; }

    public bool ShowFailureNotifications { get; set; } = true;

    public bool IsOnboardingCompleted { get; set; }

    public string Theme { get; set; } = "System";

    public string TranslationEngineId { get; set; } = TranslationEngineDefinition.OfflineStandardId;

    public bool AcceptedGemmaTerms { get; set; }

    private static string GetDefaultConfigurationLanguage()
    {
        var code = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return code is "es" or "en" or "pt" ? code : "en";
    }
}
