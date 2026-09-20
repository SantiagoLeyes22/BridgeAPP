namespace Bridge.Models;

public sealed record LanguageDefinition(string Code, string DisplayName)
{
    public static IReadOnlyList<LanguageDefinition> Supported { get; } =
    [
        new("es", "Spanish"),
        new("en", "English"),
        new("pt", "Portuguese")
    ];

    public static LanguageDefinition Spanish => Supported[0];

    public static LanguageDefinition English => Supported[1];

    public static LanguageDefinition Portuguese => Supported[2];

    public static LanguageDefinition? FindByCode(string? code) =>
        Supported.FirstOrDefault(language =>
            string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase));
}
