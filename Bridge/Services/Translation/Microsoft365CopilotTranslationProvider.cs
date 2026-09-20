using Bridge.Models;
using Bridge.Services.Authentication;
using Bridge.Services.Infrastructure;

namespace Bridge.Services.Translation;

public sealed class Microsoft365CopilotTranslationProvider : ITranslationProvider
{
    private readonly CopilotApiClient _apiClient;
    private readonly IAuthenticationService _authentication;
    private readonly AppConfiguration _configuration;

    public Microsoft365CopilotTranslationProvider(
        CopilotApiClient apiClient,
        IAuthenticationService authentication,
        AppConfiguration configuration)
    {
        _apiClient = apiClient;
        _authentication = authentication;
        _configuration = configuration;
    }

    public string ProviderName => "Microsoft 365 Copilot";

    public bool IsAvailable => _configuration.MicrosoftIdentity.IsConfigured;

    public bool IsAuthenticated => _authentication.IsAuthenticated;

    public bool SupportsTranslationStyles => true;

    public Task<TranslationResult> TranslateToPrimaryLanguageAsync(
        string text,
        string primaryLanguage,
        TranslationStyleDefinition style,
        CancellationToken cancellationToken) =>
        TranslateCoreAsync(text, primaryLanguage, style, cancellationToken);

    public Task<TranslationResult> TranslateAsync(
        string text,
        string targetLanguage,
        TranslationStyleDefinition style,
        CancellationToken cancellationToken) =>
        TranslateCoreAsync(text, targetLanguage, style, cancellationToken);

    private async Task<TranslationResult> TranslateCoreAsync(
        string text,
        string targetLanguage,
        TranslationStyleDefinition style,
        CancellationToken cancellationToken)
    {
        // A fresh conversation per translation prevents context from one support case
        // from influencing another. The conversation identifier exists only in memory.
        var conversationId = await _apiClient.CreateConversationAsync(cancellationToken);
        var prompt = TranslationPromptBuilder.Build(text, targetLanguage, style);
        var response = await _apiClient.SendMessageAsync(conversationId, prompt, cancellationToken);
        return TranslationResponseParser.Parse(response, targetLanguage);
    }
}
