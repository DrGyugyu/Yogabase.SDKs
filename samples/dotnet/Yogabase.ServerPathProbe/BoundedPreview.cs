using System;
using System.Text;
using System.Text.Json;

namespace Yogabase.Samples.ServerPathProbe;

/// <summary>
/// Truncates response bodies before they reach the console, so a run never prints more than a
/// bounded preview of a Lambda result or a realtime event payload.
/// </summary>
internal static class BoundedPreview
{
    private const string TruncationMarker = "...(truncated)";

    internal static string OfText(string? text, int maximumCharacters)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumCharacters);
        if (string.IsNullOrEmpty(text))
        {
            return "(empty)";
        }

        var oneLine = text.Replace('\r', ' ').Replace('\n', ' ');
        return oneLine.Length <= maximumCharacters
            ? oneLine
            : oneLine[..maximumCharacters] + TruncationMarker;
    }

    /// <summary>
    /// Decodes bytes as UTF-8 when they round-trip cleanly (the common case for a JSON or text
    /// Lambda result); otherwise falls back to a bounded lowercase-hex preview so binary data never
    /// reaches the console as replacement characters.
    /// </summary>
    internal static string OfBytes(byte[]? bytes, int maximumCharacters)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return "(empty)";
        }

        string decoded;
        try
        {
            decoded = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            var hexLength = Math.Max(0, (maximumCharacters / 2) * 2);
            var hexChars = Math.Min(bytes.Length * 2, hexLength);
            var preview = Convert.ToHexStringLower(bytes, 0, hexChars / 2);
            var suffix = hexChars < bytes.Length * 2 ? TruncationMarker : string.Empty;
            return $"hex:{preview}{suffix} ({bytes.Length} bytes total)";
        }

        return OfText(decoded, maximumCharacters);
    }

    internal static string OfJson(JsonElement element, int maximumCharacters) =>
        OfText(element.ValueKind == JsonValueKind.Undefined ? null : element.GetRawText(), maximumCharacters);
}
