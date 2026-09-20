namespace Bridge.Models;

public sealed record RetranslationOptions(
    LanguageDefinition TargetLanguage,
    TranslationStyleDefinition Style);
