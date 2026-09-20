using Bridge.Models;

namespace Bridge.Services.Authentication;

public interface IAuthenticationService
{
    bool IsConfigured { get; }

    bool IsAuthenticated { get; }

    AuthenticationAccount? GetCurrentAccount();

    event EventHandler? AuthenticationStateChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<AuthenticationAccount> SignInAsync(CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);

    Task<string> AcquireTokenAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);
}
