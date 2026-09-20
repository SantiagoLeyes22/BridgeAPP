using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Bridge.Models;
using Bridge.Services.Infrastructure;
using Bridge.Services.Windows;

namespace Bridge.Services.Authentication;

public sealed class MicrosoftAuthenticationService : IAuthenticationService
{
    internal static readonly string[] CopilotScopes =
    [
        "https://graph.microsoft.com/Sites.Read.All",
        "https://graph.microsoft.com/Mail.Read",
        "https://graph.microsoft.com/People.Read.All",
        "https://graph.microsoft.com/OnlineMeetingTranscript.Read.All",
        "https://graph.microsoft.com/Chat.Read",
        "https://graph.microsoft.com/ChannelMessage.Read.All",
        "https://graph.microsoft.com/ExternalItem.Read.All"
    ];

    private readonly AppConfiguration _configuration;
    private readonly IWindowHandleProvider _windowHandleProvider;
    private readonly IDiagnosticLogger _logger;
    private readonly IPublicClientApplication? _application;
    private IAccount? _account;

    public MicrosoftAuthenticationService(
        AppConfiguration configuration,
        IWindowHandleProvider windowHandleProvider,
        IDiagnosticLogger logger)
    {
        _configuration = configuration;
        _windowHandleProvider = windowHandleProvider;
        _logger = logger;

        if (!IsConfigured)
        {
            return;
        }

        var authority = $"https://login.microsoftonline.com/{Uri.EscapeDataString(configuration.MicrosoftIdentity.Tenant)}";
        var brokerOptions = new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
        {
            Title = "Bridge"
        };

        _application = PublicClientApplicationBuilder
            .Create(configuration.MicrosoftIdentity.ClientId)
            .WithAuthority(authority)
            .WithDefaultRedirectUri()
            .WithBroker(brokerOptions)
            .Build();
    }

    public bool IsConfigured => _configuration.MicrosoftIdentity.IsConfigured;

    public bool IsAuthenticated => _account is not null;

    public event EventHandler? AuthenticationStateChanged;

    public AuthenticationAccount? GetCurrentAccount() => _account is null
        ? null
        : new AuthenticationAccount(_account.Username, _account.HomeAccountId.Identifier);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_application is null)
        {
            return;
        }

        _account = (await _application.GetAccountsAsync().ConfigureAwait(false)).FirstOrDefault();
        if (_account is null)
        {
            return;
        }

        try
        {
            var result = await _application
                .AcquireTokenSilent(CopilotScopes, _account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            UpdateAccount(result.Account);
        }
        catch (MsalUiRequiredException)
        {
            _account = null;
        }
        catch (MsalException exception)
        {
            _account = null;
            _logger.Error($"Silent authentication initialization failed. MSAL={exception.ErrorCode}.");
        }
    }

    public async Task<AuthenticationAccount> SignInAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        AuthenticationResult? result = null;
        var accounts = await _application!.GetAccountsAsync().ConfigureAwait(false);
        var cachedAccount = accounts.FirstOrDefault();

        try
        {
            result = await _application
                .AcquireTokenSilent(
                    CopilotScopes,
                    cachedAccount ?? PublicClientApplication.OperatingSystemAccount)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MsalUiRequiredException)
        {
            // Interactive WAM is used only after the user explicitly chooses Sign in.
            result = await _application
                .AcquireTokenInteractive(CopilotScopes)
                .WithParentActivityOrWindow(() => _windowHandleProvider.GetMainWindowHandle())
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        UpdateAccount(result.Account);
        _logger.Info("Authentication succeeded.");
        return GetCurrentAccount()!;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        if (_application is null)
        {
            return;
        }

        var accounts = await _application.GetAccountsAsync().ConfigureAwait(false);
        foreach (var account in accounts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _application.RemoveAsync(account).ConfigureAwait(false);
        }

        _account = null;
        AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
        _logger.Info("Authentication cache entry removed.");
    }

    public async Task<string> AcquireTokenAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var account = _account ??
                      (await _application!.GetAccountsAsync().ConfigureAwait(false)).FirstOrDefault();
        if (account is null)
        {
            throw new AuthenticationRequiredException("No Microsoft 365 account is connected.");
        }

        try
        {
            var builder = _application!
                .AcquireTokenSilent(CopilotScopes, account)
                .WithForceRefresh(forceRefresh);
            var result = await builder.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            UpdateAccount(result.Account);
            return result.AccessToken;
        }
        catch (MsalUiRequiredException exception)
        {
            _account = null;
            AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
            throw new AuthenticationRequiredException(
                "The Microsoft session has expired and requires interactive sign-in.",
                exception);
        }
    }

    private void UpdateAccount(IAccount account)
    {
        var changed = !string.Equals(
            _account?.HomeAccountId.Identifier,
            account.HomeAccountId.Identifier,
            StringComparison.Ordinal);
        _account = account;
        if (changed)
        {
            AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void EnsureConfigured()
    {
        if (_application is null)
        {
            throw new InvalidOperationException(
                "Microsoft Entra authentication is not configured. Set MicrosoftIdentity.ClientId in appsettings.json.");
        }
    }
}
