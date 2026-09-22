using System.Text.Json;
using Bridge.Models;

namespace Bridge.Services.Translation;

internal static class TranslationResponseParser
{
    internal static TranslationResult Parse(string responseText, string targetLanguage)
    {
        var cleaned = StripCodeFence(responseText).Trim();
        var json = ExtractJsonObject(cleaned);
        if (json is not null)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                var translatedText = GetString(root, "translatedText");
                if (!string.IsNullOrWhiteSpace(translatedText))
                {
                    var target = LanguageDefinition.Find(targetLanguage);
                    return new TranslationResult(
                        GetString(root, "detectedLanguage") ?? "Detected language",
                        GetString(root, "detectedLanguageCode") ?? string.Empty,
                        GetString(root, "targetLanguage") ?? targetLanguage,
                        GetString(root, "targetLanguageCode") ?? target?.Code ?? string.Empty,
                        translatedText.Trim());
                }
            }
            catch (JsonException)
            {
                // Fall through to the safe text fallback below.
            }
        }

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            throw new CopilotServiceException(
                CopilotErrorKind.ParseFailure,
                "Copilot returned no usable translation.");
        }

        var fallbackTarget = LanguageDefinition.Find(targetLanguage);
        return new TranslationResult(
            "Detected language",
            string.Empty,
            targetLanguage,
            fallbackTarget?.Code ?? string.Empty,
            cleaned);
    }

    private static string StripCodeFence(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstNewLine = trimmed.IndexOf('\n');
        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewLine >= 0 && lastFence > firstNewLine
            ? trimmed[(firstNewLine + 1)..lastFence]
            : trimmed.Trim('`');
    }

    private static string? ExtractJsonObject(string value)
    {
        var start = value.IndexOf('{');
        var end = value.LastIndexOf('}');
        return start >= 0 && end > start ? value[start..(end + 1)] : null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }
}
