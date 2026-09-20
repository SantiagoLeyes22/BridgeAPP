using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bridge.Services.Authentication;
using Bridge.Services.Infrastructure;

namespace Bridge.Services.Translation;

public sealed class CopilotApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationService _authentication;
    private readonly AppConfiguration _configuration;
    private readonly IDiagnosticLogger _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public CopilotApiClient(
        HttpClient httpClient,
        IAuthenticationService authentication,
        AppConfiguration configuration,
        IDiagnosticLogger logger)
    {
        _httpClient = httpClient;
        _authentication = authentication;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> CreateConversationAsync(CancellationToken cancellationToken)
    {
        using var document = await SendAsync(
            HttpMethod.Post,
            "copilot/conversations",
            new { },
            HttpStatusCode.Created,
            cancellationToken);

        if (!TryGetString(document.RootElement, "id", out var conversationId) ||
            string.IsNullOrWhiteSpace(conversationId))
        {
            throw new CopilotServiceException(
                CopilotErrorKind.ApiCompatibility,
                "Copilot returned an unsupported conversation response.");
        }

        return conversationId;
    }

    public async Task<string> SendMessageAsync(
        string conversationId,
        string prompt,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            message = new { text = prompt },
            locationHint = new { timeZone = GetIanaTimeZone() },
            contextualResources = new
            {
                webContext = new { isWebEnabled = false }
            }
        };

        var relativeUri = $"copilot/conversations/{Uri.EscapeDataString(conversationId)}/chat";
        using var document = await SendAsync(
            HttpMethod.Post,
            relativeUri,
            request,
            HttpStatusCode.OK,
            cancellationToken);

        if (!document.RootElement.TryGetProperty("messages", out var messages) ||
            messages.ValueKind != JsonValueKind.Array)
        {
            throw new CopilotServiceException(
                CopilotErrorKind.ApiCompatibility,
                "Copilot returned an unsupported chat response.");
        }

        for (var index = messages.GetArrayLength() - 1; index >= 0; index--)
        {
            var message = messages[index];
            if (TryGetString(message, "text", out var text) && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        throw new CopilotServiceException(
            CopilotErrorKind.ParseFailure,
            "Copilot returned no usable translation text.");
    }

    private async Task<JsonDocument> SendAsync(
        HttpMethod method,
        string relativeUri,
        object body,
        HttpStatusCode expectedStatus,
        CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        var forceRefresh = false;

        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            string accessToken;
            try
            {
                accessToken = await _authentication
                    .AcquireTokenAsync(forceRefresh, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (AuthenticationRequiredException exception)
            {
                throw new CopilotServiceException(
                    CopilotErrorKind.SessionExpired,
                    "The Microsoft session has expired.",
                    innerException: exception);
            }

            using var request = new HttpRequestMessage(method, relativeUri)
            {
                Content = JsonContent.Create(body, options: _jsonOptions)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new CopilotServiceException(
                    CopilotErrorKind.Timeout,
                    "The Copilot request timed out.");
            }
            catch (HttpRequestException exception)
            {
                throw new CopilotServiceException(
                    CopilotErrorKind.NetworkUnavailable,
                    "Unable to reach Microsoft 365 Copilot.",
                    innerException: exception);
            }

            using (response)
            {
                if (response.StatusCode == expectedStatus)
                {
                    try
                    {
                        await using var responseStream = await response.Content
                            .ReadAsStreamAsync(cancellationToken)
                            .ConfigureAwait(false);
                        return await JsonDocument
                            .ParseAsync(responseStream, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (JsonException exception)
                    {
                        throw new CopilotServiceException(
                            CopilotErrorKind.ApiCompatibility,
                            "Copilot returned an unsupported JSON response.",
                            (int)response.StatusCode,
                            exception);
                    }
                }

                _logger.Error($"Copilot request returned HTTP {(int)response.StatusCode}.");

                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt < maximumAttempts)
                {
                    forceRefresh = true;
                    continue;
                }

                if (IsRetryable(response.StatusCode) && attempt < maximumAttempts)
                {
                    var delay = GetRetryDelay(response, attempt);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw CreateHttpException(response.StatusCode);
            }
        }

        throw new CopilotServiceException(
            CopilotErrorKind.Unknown,
            "The Copilot request could not be completed.");
    }

    private CopilotServiceException CreateHttpException(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => new CopilotServiceException(
            CopilotErrorKind.SessionExpired,
            "The Microsoft session has expired.",
            (int)statusCode),
        HttpStatusCode.Forbidden => new CopilotServiceException(
            CopilotErrorKind.LicenseUnavailable,
            "The account does not have access to Microsoft 365 Copilot, or organizational consent is missing.",
            (int)statusCode),
        HttpStatusCode.TooManyRequests => new CopilotServiceException(
            CopilotErrorKind.RateLimited,
            "Microsoft 365 Copilot is temporarily rate limited.",
            (int)statusCode),
        HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout => new CopilotServiceException(
            CopilotErrorKind.Timeout,
            "The Copilot request timed out.",
            (int)statusCode),
        _ => new CopilotServiceException(
            CopilotErrorKind.ApiCompatibility,
            "Microsoft 365 Copilot returned an unsupported response.",
            (int)statusCode)
    };

    private TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter?.Delta;
        if (retryAfter is null && response.Headers.RetryAfter?.Date is { } date)
        {
            retryAfter = date - DateTimeOffset.UtcNow;
        }

        var maximum = TimeSpan.FromSeconds(Math.Max(1, _configuration.Copilot.MaximumRetryDelaySeconds));
        var delay = retryAfter.GetValueOrDefault(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));
        return delay <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : delay > maximum ? maximum : delay;
    }

    private static bool IsRetryable(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.BadGateway or
            HttpStatusCode.GatewayTimeout;

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.String &&
               (value = property.GetString() ?? string.Empty).Length > 0;
    }

    private static string GetIanaTimeZone()
    {
        var localId = TimeZoneInfo.Local.Id;
        return TimeZoneInfo.TryConvertWindowsIdToIanaId(localId, out var ianaId)
            ? ianaId
            : "Etc/UTC";
    }
}
