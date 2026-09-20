using Bridge.Models;

namespace Bridge.Services.Translation;

public interface ITranslationEngineService
{
    IReadOnlyList<TranslationEngineDefinition> Engines { get; }

    HardwareProfile Hardware { get; }

    TranslationEngineDefinition SelectedEngine { get; }

    Task<TranslationEngineStatus> GetStatusAsync(string engineId, CancellationToken cancellationToken = default);

    Task PrepareAsync(
        string engineId,
        IProgress<ModelPreparationProgress>? progress,
        CancellationToken cancellationToken = default);
}
