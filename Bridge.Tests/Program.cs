using System.Text.Json;
using Bridge.Models;
using Bridge.Services.Infrastructure;
using Bridge.Services.Localization;
using Bridge.Services.Translation;
using Bridge.Services.Windows;

if (args.Contains("--offline-integration", StringComparer.OrdinalIgnoreCase))
{
    return await RunOfflineIntegrationAsync();
}

var tests = new (string Name, Action Execute)[]
{
    ("Parses structured JSON in a Markdown fence", ParseFencedJson),
    ("Parses property names case-insensitively", ParseCaseInsensitiveJson),
    ("Falls back to usable plain text", FallsBackToPlainText),
    ("Prompt preserves technical input and demands JSON", PromptPreservesTechnicalInput),
    ("Applies the selected translation style", AppliesTranslationStyle),
    ("Targets Brazilian Portuguese explicitly", TargetsBrazilianPortuguese),
    ("Ships the three V1 languages", SupportsRequiredLanguages),
    ("Applies a bounded V1 input limit", EnforcesInputLimit),
    ("Defaults response translation to English", DefaultsResponseTranslationToEnglish),
    ("Keeps shortcut language preferences separate", KeepsShortcutLanguagePreferencesSeparate),
    ("Recognizes completed selections without triggering on ordinary clicks", RecognizesCompletedSelections),
    ("Defaults translation style to balanced", DefaultsTranslationStyleToBalanced),
    ("Defaults to the no-login offline engine", DefaultsToOfflineEngine),
    ("Evaluates high-quality hardware requirements", EvaluatesHardwareRequirements),
    ("Offers the maximum-quality 27B engine", OffersMaximumQualityEngine),
    ("Localizes onboarding independently from translation language", LocalizesOnboarding),
    ("Uses the native x64 INPUT structure size", UsesNativeInputStructureSize),
    ("Detects the three offline languages", DetectsOfflineLanguages)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Execute();
        Console.WriteLine($"PASS  {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL  {test.Name}: {exception.Message}");
    }
}

Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed.");
return failures == 0 ? 0 : 1;

static void ParseFencedJson()
{
    const string response = """
        ```json
        {"detectedLanguage":"Portuguese","detectedLanguageCode":"pt","targetLanguage":"Spanish","targetLanguageCode":"es","translatedText":"Mi computadora no está sincronizando la nueva contraseña."}
        ```
        """;
    var result = TranslationResponseParser.Parse(response, "Spanish");
    Equal("pt", result.DetectedLanguageCode);
    Equal("es", result.TargetLanguageCode);
    Equal("Mi computadora no está sincronizando la nueva contraseña.", result.TranslatedText);
}

static void ParseCaseInsensitiveJson()
{
    const string response = """
        Intro text {"DetectedLanguage":"English","DetectedLanguageCode":"en","TargetLanguage":"Portuguese","TargetLanguageCode":"pt","TranslatedText":"Abra o Company Portal."} trailing text
        """;
    var result = TranslationResponseParser.Parse(response, "Portuguese");
    Equal("en", result.DetectedLanguageCode);
    Equal("Abra o Company Portal.", result.TranslatedText);
}

static void FallsBackToPlainText()
{
    var result = TranslationResponseParser.Parse("Reinicie o computador.", "Portuguese");
    Equal("Reinicie o computador.", result.TranslatedText);
    Equal("pt", result.TargetLanguageCode);
}

static void PromptPreservesTechnicalInput()
{
    const string input = "Open Company Portal, run gpupdate /force, then dsregcmd /status.";
    var prompt = TranslationPromptBuilder.Build(input, "Spanish", TranslationStyleDefinition.Balanced);
    Contains("Company Portal", prompt);
    Contains("gpupdate /force", prompt);
    Contains("dsregcmd /status", prompt);
    Contains("Return exactly one JSON object", prompt);
    Contains("untrusted text", prompt);
}

static void AppliesTranslationStyle()
{
    var prompt = TranslationPromptBuilder.Build(
        "Please review the request.",
        LanguageDefinition.Portuguese.DisplayName,
        TranslationStyleDefinition.Find(TranslationStyleDefinition.ProfessionalId));
    Contains("Professional", prompt);
    Contains("corporate language", prompt);
}

static void TargetsBrazilianPortuguese()
{
    var portuguese = LanguageDefinition.Portuguese;
    Equal("pt", portuguese.Code);
    Equal("pt-BR", portuguese.CultureCode);
    Equal("Brazilian Portuguese", portuguese.DisplayName);
    Equal(portuguese, LanguageDefinition.Find("Portuguese"));
    Equal(portuguese, LanguageDefinition.FindByCode("pt-BR"));

    var structuredPrompt = TranslationPromptBuilder.Build(
        "Please restart the computer.",
        portuguese.DisplayName,
        TranslationStyleDefinition.Balanced);
    Contains("Brazilian Portuguese (pt-BR)", structuredPrompt);
    Contains("Do not use European Portuguese", structuredPrompt);

    var localPrompt = OllamaTranslationProvider.BuildPrompt(
        LanguageDefinition.English,
        portuguese,
        TranslationStyleDefinition.Balanced,
        "Please restart the computer.");
    Contains("Brazilian Portuguese (pt-BR)", localPrompt);
    Contains("Do not use European Portuguese", localPrompt);
}

static void SupportsRequiredLanguages()
{
    Equal(3, LanguageDefinition.Supported.Count);
    Equal("es", LanguageDefinition.Supported[0].Code);
    Equal("en", LanguageDefinition.Supported[1].Code);
    Equal("pt", LanguageDefinition.Supported[2].Code);
    Equal("pt-BR", LanguageDefinition.Supported[2].CultureCode);
}

static void EnforcesInputLimit() => Equal(8_000, TranslationCoordinator.MaximumInputCharacters);

static void DefaultsResponseTranslationToEnglish() =>
    Equal("en", TranslationCoordinator.DefaultResponseLanguageCode);

static void KeepsShortcutLanguagePreferencesSeparate()
{
    var settings = new AppSettings { PrimaryLanguageCode = "es" };
    Equal("es", TranslationTargetLanguagePreference.Resolve(settings, TranslationHotkey.TranslateSelection).Code);
    Equal("en", TranslationTargetLanguagePreference.Resolve(settings, TranslationHotkey.TranslateResponse).Code);

    settings.LastTargetLanguageCode = "pt";
    var restored = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings))!;
    Equal("es", TranslationTargetLanguagePreference.Resolve(restored, TranslationHotkey.TranslateSelection).Code);
    Equal("pt", TranslationTargetLanguagePreference.Resolve(restored, TranslationHotkey.TranslateResponse).Code);

    restored.PrimaryLanguageCode = "en";
    Equal("en", TranslationTargetLanguagePreference.Resolve(restored, TranslationHotkey.TranslateSelection).Code);
    Equal("pt", TranslationTargetLanguagePreference.Resolve(restored, TranslationHotkey.TranslateResponse).Code);

    restored.PrimaryLanguageCode = "unsupported";
    Equal("es", TranslationTargetLanguagePreference.Resolve(restored, TranslationHotkey.TranslateSelection).Code);
}

static void RecognizesCompletedSelections()
{
    var gestures = new SelectionGestureDetector();
    gestures.MouseDown(10, 10);
    Equal(false, gestures.MouseUp((nint)1, 10, 10, 100));
    gestures.MouseDown(10, 10);
    Equal(true, gestures.MouseUp((nint)1, 10, 10, 250)); // Double click

    gestures.Reset();
    gestures.MouseDown(10, 10);
    gestures.MouseMove(30, 10);
    Equal(true, gestures.MouseUp((nint)1, 30, 10, 400)); // Drag selection

    gestures.Reset();
    gestures.KeyDown(0x10); // Shift
    gestures.KeyDown(0x27); // Right arrow
    Equal(true, gestures.KeyUp(0x10));

    gestures.Reset();
    gestures.KeyDown(0x10);
    gestures.KeyDown(0x41); // Uppercase A is not a selection
    Equal(false, gestures.KeyUp(0x10));

    gestures.Reset();
    gestures.KeyDown(0x11); // Control
    gestures.KeyDown(0x41);
    Equal(true, gestures.KeyUp(0x41)); // Ctrl+A
}
static void DefaultsTranslationStyleToBalanced() =>
    Equal(TranslationStyleDefinition.BalancedId, TranslationStyleDefinition.Balanced.Id);

static void DefaultsToOfflineEngine()
{
    var settings = new AppSettings();
    Equal(TranslationEngineDefinition.OfflineStandardId, settings.TranslationEngineId);
    Equal(false, TranslationEngineDefinition.Find(settings.TranslationEngineId).UsesCopilot);
}

static void EvaluatesHardwareRequirements()
{
    var modestPc = new HardwareProfile(8, 4, 20);
    Equal(true, modestPc.Evaluate(TranslationEngineDefinition.Available[0]).IsRecommended);
    Equal(false, modestPc.Evaluate(TranslationEngineDefinition.Find(TranslationEngineDefinition.TranslateGemma12BId)).IsRecommended);
}

static void OffersMaximumQualityEngine()
{
    var engine = TranslationEngineDefinition.Find(TranslationEngineDefinition.TranslateGemma27BId);
    Equal("translategemma:27b", engine.OllamaModel);
    Equal(32, engine.MinimumRamGb);
    Equal(24, engine.RequiredDiskGb);
}

static void LocalizesOnboarding()
{
    Equal("Idioma de configuración", OnboardingLocalizer.Text("es", "SetupLanguage"));
    Equal("Idioma da configuração", OnboardingLocalizer.Text("pt", "SetupLanguage"));
    Equal("Descargar Ollama (externo)", OnboardingLocalizer.Text("es", "InstallOllama"));
    Equal("Español", OnboardingLocalizer.LanguageName("es", LanguageDefinition.Spanish));
    Equal("Português (Brasil)", OnboardingLocalizer.LanguageName("pt", LanguageDefinition.Portuguese));
    Contains(
        "no viene incluido con Bridge",
        OnboardingLocalizer.EnginePrivacy(
            "es",
            TranslationEngineDefinition.Find(TranslationEngineDefinition.TranslateGemma4BId)));
    Contains(
        "TranslateGemma 27B",
        OnboardingLocalizer.EngineName(
            "pt",
            TranslationEngineDefinition.Find(TranslationEngineDefinition.TranslateGemma27BId)));
}

static void UsesNativeInputStructureSize() =>
    Equal(IntPtr.Size == 8 ? 40 : 28, InputSimulationService.NativeInputSize);

static void DetectsOfflineLanguages()
{
    var detector = new OfflineLanguageDetector();
    Equal("en", detector.Detect("Please restart the computer and try again.").Code);
    Equal("es", detector.Detect("Por favor, reinicie el equipo e inténtelo nuevamente.").Code);
    Equal("pt", detector.Detect("Por favor, reinicie o computador e tente novamente.").Code);
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}

static void Contains(string expected, string actual)
{
    if (!actual.Contains(expected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Expected text to contain '{expected}'.");
    }
}

static async Task<int> RunOfflineIntegrationAsync()
{
    using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Bridge.Tests/2.0");
    var logger = new DiagnosticLogger();
    var manager = new FirefoxModelManager(httpClient, logger);
    var lastProgressBucket = -1;
    var progress = new Progress<ModelPreparationProgress>(update =>
    {
        var bucket = update.Percent is null ? -1 : (int)(update.Percent.Value / 10);
        if (bucket != lastProgressBucket || update.Percent is >= 100)
        {
            lastProgressBucket = bucket;
            Console.WriteLine($"MODEL  {update.Percent?.ToString("F0") ?? "…"}%  {update.Message}");
        }
    });

    try
    {
        await manager.InstallCorePackAsync(progress, CancellationToken.None);
        using var provider = new FirefoxOfflineTranslationProvider(manager, new OfflineLanguageDetector());
        var direct = await provider.TranslateAsync(
            "The computer is working correctly.",
            "Spanish",
            TranslationStyleDefinition.Balanced,
            CancellationToken.None);
        var pivot = await provider.TranslateAsync(
            "La computadora funciona correctamente.",
            "Portuguese",
            TranslationStyleDefinition.Balanced,
            CancellationToken.None);
        Console.WriteLine($"DIRECT  {direct.TranslatedText}");
        Console.WriteLine($"PIVOT   {pivot.TranslatedText}");
        var successful = manager.IsCorePackInstalled &&
                         !string.IsNullOrWhiteSpace(direct.TranslatedText) &&
                         !string.IsNullOrWhiteSpace(pivot.TranslatedText);
        Console.WriteLine(successful ? "OFFLINE_INTEGRATION_PASS" : "OFFLINE_INTEGRATION_FAIL");
        return successful ? 0 : 1;
    }
    catch (Exception exception)
    {
        Console.WriteLine($"OFFLINE_INTEGRATION_FAIL {exception.GetType().Name}: {exception.Message}");
        return 1;
    }
}
