using System.Text;
using System.Text.Json;

namespace ImprovisedEosl.Core;

public sealed record JsonPayloadValidation(
    bool IsValid,
    string? Json,
    string? ErrorCode,
    int Utf8Bytes);

public static class JsonPayloadPolicy
{
    public const int MaxPayloadUtf8Bytes = 1024 * 1024;
    public const int MaxJsonDepth = 64;
    public const int MaxTransportUtf8Bytes = (MaxPayloadUtf8Bytes * 6) + 4096;

    public static JsonPayloadValidation ValidateArguments(string? json)
    {
        return Validate(json, allowUndefined: false);
    }

    public static JsonPayloadValidation ValidateReturnValue(string? json)
    {
        return Validate(json ?? "undefined", allowUndefined: true);
    }

    public static string CreateFailureJson(string kind, JsonPayloadValidation validation)
    {
        return JsonSerializer.Serialize(new
        {
            kind,
            ok = false,
            reason = validation.ErrorCode,
            utf8Bytes = validation.Utf8Bytes,
            maxUtf8Bytes = MaxPayloadUtf8Bytes
        });
    }

    public static string Summarize(string? value, int maxCharacters = 512)
    {
        if (value is null)
        {
            return "(null)";
        }

        var bytes = Encoding.UTF8.GetByteCount(value);
        if (value.Length <= maxCharacters)
        {
            return $"{value} [utf8Bytes={bytes}]";
        }

        return $"{value[..maxCharacters]}... [truncated; chars={value.Length}; utf8Bytes={bytes}]";
    }

    private static JsonPayloadValidation Validate(string? json, bool allowUndefined)
    {
        if (json is null)
        {
            return new JsonPayloadValidation(false, null, "missing", 0);
        }

        var utf8Bytes = Encoding.UTF8.GetByteCount(json);
        if (utf8Bytes > MaxPayloadUtf8Bytes)
        {
            return new JsonPayloadValidation(false, null, "too-large", utf8Bytes);
        }

        if (allowUndefined && json == "undefined")
        {
            return new JsonPayloadValidation(true, json, null, utf8Bytes);
        }

        try
        {
            using var document = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = MaxJsonDepth
                });
            return new JsonPayloadValidation(true, json, null, utf8Bytes);
        }
        catch (JsonException)
        {
            return new JsonPayloadValidation(false, null, "invalid-json", utf8Bytes);
        }
    }
}
