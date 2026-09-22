namespace Bridge.Models;

public sealed class AppSettings
{
    public string ConfigurationLanguageCode { get; set; } = GetDefaultConfigurationLanguage();

    public string PrimaryLanguageCode { get; set; } = "es";

    public string? LastTargetLanguageCode { get; set; }

    public bool StartWithWindows { get; set; }

    public bool ShowFailureNotifications { get; set; } = true;

    public bool IsOnboardingCompleted { get; set; }

    public string Theme { get; set; } = "System";

    public string TranslationEngineId { get; set; } = TranslationEngineDefinition.OfflineStandardId;

    public bool AcceptedGemmaTerms { get; set; }

    private static string GetDefaultConfigurationLanguage()
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        if (culture.Name.Equals("pt-BR", StringComparison.OrdinalIgnoreCase) ||
            culture.TwoLetterISOLanguageName.Equals("pt", StringComparison.OrdinalIgnoreCase))
        {
            // Brazilian Portuguese is the supported Portuguese UI variant.
            return "pt";
        }

        var code = culture.TwoLetterISOLanguageName;
        return code is "es" or "en" or "pt" ? code : "en";
    }
}
