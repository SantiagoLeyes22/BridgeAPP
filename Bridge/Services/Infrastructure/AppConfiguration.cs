using System.Text.Json;

namespace Bridge.Services.Infrastructure;

public sealed class AppConfiguration
{
    public MicrosoftIdentityConfiguration MicrosoftIdentity { get; init; } = new();

    public CopilotConfiguration Copilot { get; init; } = new();

    public static AppConfiguration Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return new AppConfiguration();
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<AppConfiguration>(
                   stream,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new AppConfiguration();
    }
}

public sealed class MicrosoftIdentityConfiguration
{
    public string ClientId { get; init; } = "YOUR_ENTRA_APPLICATION_CLIENT_ID";

    public string Tenant { get; init; } = "organizations";

    public bool IsConfigured =>
        Guid.TryParse(ClientId, out var id) && id != Guid.Empty &&
        !string.Equals(ClientId, "YOUR_ENTRA_APPLICATION_CLIENT_ID", StringComparison.Ordinal);
}

public sealed class CopilotConfiguration
{
    public string GraphBaseUrl { get; init; } = "https://graph.microsoft.com/beta/";

    public int RequestTimeoutSeconds { get; init; } = 45;

    public int MaximumRetryDelaySeconds { get; init; } = 15;
}
