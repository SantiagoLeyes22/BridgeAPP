namespace Bridge.Models;

public sealed record TranslationEngineDefinition(
    string Id,
    string DisplayName,
    string ShortDescription,
    string PrivacyDescription,
    int MinimumRamGb,
    int RecommendedCpuCores,
    int RequiredDiskGb,
    string GpuRecommendation,
    int TimeoutSeconds,
    string? OllamaModel = null)
{
    public const string OfflineStandardId = "offline-standard";
    public const string TranslateGemma4BId = "translategemma-4b";
    public const string TranslateGemma12BId = "translategemma-12b";
    public const string TranslateGemma27BId = "translategemma-27b";
    public const string CopilotId = "microsoft-copilot";

    public bool UsesGemma => OllamaModel is not null;

    public bool UsesCopilot => Id == CopilotId;

    public string Requirements =>
        $"RAM: {MinimumRamGb} GB or more · CPU: {RecommendedCpuCores}+ logical cores · " +
        $"Free disk: {RequiredDiskGb} GB · GPU: {GpuRecommendation}";

    public static IReadOnlyList<TranslationEngineDefinition> Available { get; } =
    [
        new(
            OfflineStandardId,
            "Offline standard — recommended",
            "Fast private translation for English, Spanish, and Portuguese using Firefox/Bergamot models.",
            "Text stays on this PC. Internet is used only once to download the language pack.",
            4,
            2,
            1,
            "Not required",
            60),
        new(
            TranslateGemma4BId,
            "Offline high quality — TranslateGemma 4B",
            "Better context and natural phrasing on typical business laptops. The model download is about 3.3 GB.",
            "Text stays on this PC and is processed through Ollama. Ollama is an external component and is not included with Bridge.",
            12,
            4,
            6,
            "Optional; 4 GB VRAM improves speed",
            180,
            "translategemma:4b"),
        new(
            TranslateGemma12BId,
            "Offline high quality — TranslateGemma 12B",
            "Stronger context and phrasing for demanding translations. The model download is about 8.1 GB.",
            "Text stays on this PC and is processed through Ollama. Ollama is an external component and is not included with Bridge.",
            24,
            8,
            12,
            "10 GB VRAM recommended; CPU mode is slower",
            300,
            "translategemma:12b"),
        new(
            TranslateGemma27BId,
            "Offline maximum quality — TranslateGemma 27B",
            "The strongest TranslateGemma option in the app. The model download is about 17 GB and is intended for powerful workstations.",
            "Text stays on this PC and is processed through Ollama. Ollama is an external component and is not included with Bridge.",
            32,
            12,
            24,
            "20 GB VRAM recommended; CPU mode is very slow",
            480,
            "translategemma:27b"),
        new(
            CopilotId,
            "Microsoft 365 Copilot — optional",
            "Uses the existing Microsoft 365 Copilot integration when the organization has approved and licensed it.",
            "Selected text is sent to Microsoft only when a translation is requested.",
            4,
            2,
            1,
            "Not required",
            60)
    ];

    public static TranslationEngineDefinition Find(string? id) =>
        Available.FirstOrDefault(engine => string.Equals(engine.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? Available[0];
}
