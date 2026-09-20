using Lingua;
using Bridge.Models;

namespace Bridge.Services.Translation;

public sealed class OfflineLanguageDetector
{
    private readonly LanguageDetector _detector = LanguageDetectorBuilder
        .FromLanguages(Language.English, Language.Spanish, Language.Portuguese)
        .WithPreloadedLanguageModels()
        .Build();

    public LanguageDefinition Detect(string text)
    {
        var language = _detector.DetectLanguageOf(text);
        return language switch
        {
            Language.Portuguese => LanguageDefinition.FindByCode("pt")!,
            Language.Spanish => LanguageDefinition.FindByCode("es")!,
            _ => LanguageDefinition.FindByCode("en")!
        };
    }
}
