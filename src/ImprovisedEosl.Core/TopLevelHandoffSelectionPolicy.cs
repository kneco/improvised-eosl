namespace ImprovisedEosl.Core;

public sealed record TopLevelHandoffSelection(
    bool IsAccepted,
    Uri? TargetUri,
    string? ParentOrigin,
    string Reason);

public static class TopLevelHandoffSelectionPolicy
{
    public static TopLevelHandoffSelection Select(
        Uri? currentDocument,
        string? requestedUrl,
        bool hasPendingChild)
    {
        if (hasPendingChild)
        {
            return new TopLevelHandoffSelection(false, null, null, "additional-child");
        }

        var target = DialogNavigationPolicy.Validate(requestedUrl);
        if (!target.IsValid || target.Uri is null)
        {
            return new TopLevelHandoffSelection(
                false,
                null,
                null,
                "invalid-target:" + target.ErrorCode);
        }

        if (!HostOriginGuard.IsSameOrigin(currentDocument, target.Uri))
        {
            return new TopLevelHandoffSelection(false, null, null, "cross-origin");
        }

        var parentOrigin = CompatibilityOriginPolicy.GetOrigin(currentDocument!);
        return new TopLevelHandoffSelection(true, target.Uri, parentOrigin, "accepted");
    }

    public static bool CanApply(Uri? currentDocument, string? capturedParentOrigin) =>
        HostOriginGuard.IsClaimedOriginCurrent(currentDocument, capturedParentOrigin);
}
