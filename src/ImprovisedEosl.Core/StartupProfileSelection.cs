namespace ImprovisedEosl.Core;

public enum StartupProfileSelectionError
{
    MissingId,
    MultipleSelections,
    UnknownProfile
}

public sealed record StartupProfileSelectionResult(
    CompatibilityProfile? Profile,
    StartupProfileSelectionError? Error,
    string? RequestedId)
{
    public bool IsSpecified => Profile is not null || Error is not null;
}

public static class StartupProfileSelection
{
    private const string ProfileOption = "--profile";

    public static StartupProfileSelectionResult Resolve(
        IReadOnlyList<string> arguments,
        IReadOnlyList<CompatibilityProfile> profiles)
    {
        var requestedIds = new List<string?>();
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (argument.Equals(ProfileOption, StringComparison.OrdinalIgnoreCase))
            {
                var value = index + 1 < arguments.Count &&
                    !arguments[index + 1].StartsWith("--", StringComparison.Ordinal)
                    ? arguments[++index]
                    : null;
                requestedIds.Add(value);
                continue;
            }

            if (argument.StartsWith(ProfileOption + "=", StringComparison.OrdinalIgnoreCase))
            {
                requestedIds.Add(argument[(ProfileOption.Length + 1)..]);
            }
        }

        if (requestedIds.Count == 0)
        {
            return new StartupProfileSelectionResult(null, null, null);
        }

        if (requestedIds.Count > 1)
        {
            return new StartupProfileSelectionResult(
                null,
                StartupProfileSelectionError.MultipleSelections,
                null);
        }

        var requestedId = requestedIds[0]?.Trim();
        if (string.IsNullOrEmpty(requestedId))
        {
            return new StartupProfileSelectionResult(
                null,
                StartupProfileSelectionError.MissingId,
                null);
        }

        var profile = profiles.FirstOrDefault(candidate =>
            candidate.Id.Equals(requestedId, StringComparison.OrdinalIgnoreCase));
        return profile is null
            ? new StartupProfileSelectionResult(
                null,
                StartupProfileSelectionError.UnknownProfile,
                requestedId)
            : new StartupProfileSelectionResult(profile, null, requestedId);
    }
}
