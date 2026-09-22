namespace Bridge.Models;

public sealed record LanguageDefinition(string Code, string DisplayName, string CultureCode)
{
    public static IReadOnlyList<LanguageDefinition> Supported { get; } =
    [
        new("es", "Spanish", "es"),
        new("en", "English", "en"),
        // Keep the engine code as "pt" for existing settings and Mozilla's en-pt/pt-en models.
        new("pt", "Brazilian Portuguese", "pt-BR")
    ];

    public static LanguageDefinition Spanish => Supported[0];

    public static LanguageDefinition English => Supported[1];

    public static LanguageDefinition Portuguese => Supported[2];

    public static LanguageDefinition? FindByCode(string? code) =>
        Supported.FirstOrDefault(language =>
            string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(language.CultureCode, code, StringComparison.OrdinalIgnoreCase));

    public static LanguageDefinition? Find(string? value) =>
        Supported.FirstOrDefault(language =>
            string.Equals(language.Code, value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(language.CultureCode, value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(language.DisplayName, value, StringComparison.OrdinalIgnoreCase) ||
            language.Code == "pt" && string.Equals(value, "Portuguese", StringComparison.OrdinalIgnoreCase));
}
