using System.Text.Json;

namespace ImprovisedEosl.Core;

public sealed record DialogNavigationValidation(
    bool IsValid,
    Uri? Uri,
    string? ErrorCode);

public static class DialogNavigationPolicy
{
    public const int MaxUrlCharacters = 8192;

    public static DialogNavigationValidation Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new DialogNavigationValidation(false, null, "missing");
        }

        if (value.Length > MaxUrlCharacters)
        {
            return new DialogNavigationValidation(false, null, "too-long");
        }

        if (!System.Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return new DialogNavigationValidation(false, null, "invalid-url");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return new DialogNavigationValidation(false, null, "unsupported-scheme");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return new DialogNavigationValidation(false, null, "userinfo-not-allowed");
        }

        return new DialogNavigationValidation(true, uri, null);
    }

    public static string CreateFailureJson(
        DialogNavigationValidation validation,
        string kind = "invalid-dialog-url")
    {
        return JsonSerializer.Serialize(new
        {
            kind,
            ok = false,
            reason = validation.ErrorCode,
            maxCharacters = MaxUrlCharacters
        });
    }

    public static string FormatForLog(string? value)
    {
        var validation = Validate(value);
        if (!validation.IsValid || validation.Uri is null)
        {
            return $"(rejected:{validation.ErrorCode})";
        }

        var uri = validation.Uri;
        var path = uri.GetLeftPart(UriPartial.Path);
        return path.Length <= 512 ? path : path[..512] + "...";
    }
}
