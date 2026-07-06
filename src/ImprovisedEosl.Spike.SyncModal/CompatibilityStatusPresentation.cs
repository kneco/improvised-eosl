using ImprovisedEosl.Core;

namespace ImprovisedEosl.Spike.SyncModal;

public enum CompatibilityStatusIcon
{
    Undecided,
    DetectionPending,
    Enabled,
    Denied,
    Blocked,
    Operational,
    Error
}

public enum CompatibilityOperationalStatus
{
    Initializing,
    Recovering,
    RecoveryFailed
}

public sealed record CompatibilityStatusPresentation(
    string ShortLabel,
    CompatibilityStatusIcon Icon,
    string AccessibleText,
    string DetailText);

public static class CompatibilityStatusPresentationPolicy
{
    public static CompatibilityStatusPresentation Create(CompatibilityStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        var (labelKey, icon) = status.State switch
        {
            CompatibilityStatusState.Undecided =>
                (UiText.CompatibilityStatusUndecided, CompatibilityStatusIcon.Undecided),
            CompatibilityStatusState.DetectionPending =>
                (UiText.CompatibilityStatusDetected, CompatibilityStatusIcon.DetectionPending),
            CompatibilityStatusState.Enabled =>
                (UiText.CompatibilityStatusEnabled, CompatibilityStatusIcon.Enabled),
            CompatibilityStatusState.Denied =>
                (UiText.CompatibilityStatusDenied, CompatibilityStatusIcon.Denied),
            CompatibilityStatusState.Blocked =>
                (UiText.CompatibilityStatusBlocked, CompatibilityStatusIcon.Blocked),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status.State, "Unknown compatibility status")
        };

        var shortLabel = UiText.Get(labelKey);
        var detailText = UiText.Format(
            UiText.CompatibilityStatusDetail,
            shortLabel,
            status.Origin,
            FormatApis(status.EnabledApis),
            FormatApis(status.DeniedApis),
            FormatApis(status.DetectedApis));

        return new CompatibilityStatusPresentation(shortLabel, icon, detailText, detailText);
    }

    public static CompatibilityStatusPresentation CreateOperational(CompatibilityOperationalStatus status)
    {
        var (labelKey, detailKey, icon) = status switch
        {
            CompatibilityOperationalStatus.Initializing =>
                (UiText.CompatibilityStatusChecking, UiText.CompatibilityStatusInitializingDetail,
                    CompatibilityStatusIcon.Operational),
            CompatibilityOperationalStatus.Recovering =>
                (UiText.CompatibilityStatusChecking, UiText.CompatibilityStatusRecoveringDetail,
                    CompatibilityStatusIcon.Operational),
            CompatibilityOperationalStatus.RecoveryFailed =>
                (UiText.CompatibilityStatusError, UiText.CompatibilityStatusRecoveryFailedDetail,
                    CompatibilityStatusIcon.Error),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown operational status")
        };

        var shortLabel = UiText.Get(labelKey);
        var detailText = UiText.Get(detailKey);
        return new CompatibilityStatusPresentation(shortLabel, icon, detailText, detailText);
    }

    private static string FormatApis(IReadOnlyList<string> apiNames) =>
        apiNames.Count == 0
            ? UiText.Get(UiText.CompatibilityStatusNoApis)
            : string.Join(", ", apiNames);
}
