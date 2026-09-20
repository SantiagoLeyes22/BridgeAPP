namespace Bridge.Services.Translation;

public enum CopilotErrorKind
{
    Unknown,
    LicenseUnavailable,
    AdminConsentRequired,
    NetworkUnavailable,
    SessionExpired,
    RateLimited,
    Timeout,
    ApiCompatibility,
    ParseFailure
}

public sealed class CopilotServiceException(
    CopilotErrorKind kind,
    string message,
    int? statusCode = null,
    Exception? innerException = null)
    : Exception(message, innerException)
{
    public CopilotErrorKind Kind { get; } = kind;

    public int? StatusCode { get; } = statusCode;
}
