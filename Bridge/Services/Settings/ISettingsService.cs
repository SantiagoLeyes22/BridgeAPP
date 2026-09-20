using Bridge.Models;

namespace Bridge.Services.Settings;

public interface ISettingsService
{
    AppSettings Settings { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
