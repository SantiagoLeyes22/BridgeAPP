namespace Bridge.Services.Translation;

public sealed class TranslationEngineException : Exception
{
    public TranslationEngineException(string message)
        : base(message)
    {
    }

    public TranslationEngineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
