namespace Bridge.Models;

public sealed record TranslationStyleDefinition(
    string Id,
    string DisplayName,
    string PromptInstruction)
{
    public const string NaturalId = "natural";
    public const string BalancedId = "balanced";
    public const string ProfessionalId = "professional";

    public static IReadOnlyList<TranslationStyleDefinition> Supported { get; } =
    [
        new(
            NaturalId,
            "Natural",
            "Use fluent, idiomatic, conversational language that reads as if written by a native speaker."),
        new(
            BalancedId,
            "Balanced",
            "Balance fidelity with natural phrasing, preserving the source tone and level of formality."),
        new(
            ProfessionalId,
            "Professional",
            "Use polished, concise corporate language suitable for professional workplace communication without adding or changing meaning.")
    ];

    public static TranslationStyleDefinition Balanced => Supported[1];

    public static TranslationStyleDefinition Find(string? id) =>
        Supported.FirstOrDefault(style =>
            string.Equals(style.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Balanced;
}
