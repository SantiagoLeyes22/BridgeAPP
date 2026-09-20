namespace Bridge.Services.Authentication;

public sealed class AuthenticationRequiredException(string message, Exception? innerException = null)
    : Exception(message, innerException);
