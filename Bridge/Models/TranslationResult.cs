namespace Bridge.Models;

public sealed record TranslationResult(
    string DetectedLanguage,
    string DetectedLanguageCode,
    string TargetLanguage,
    string TargetLanguageCode,
    string TranslatedText);
