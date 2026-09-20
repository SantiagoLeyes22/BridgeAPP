namespace Bridge.Models;

public sealed record TranslationEngineStatus(bool IsReady, string Message);

public sealed record ModelPreparationProgress(string Message, double? Percent = null);
