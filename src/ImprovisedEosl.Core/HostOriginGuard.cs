namespace ImprovisedEosl.Core;

public static class HostOriginGuard
{
    public static bool IsClaimedOriginCurrent(Uri? currentDocument, string? claimedOrigin)
    {
        if (currentDocument is null || claimedOrigin is null)
        {
            return false;
        }

        var actual = CompatibilityOriginPolicy.GetOrigin(currentDocument);
        var claimed = CompatibilityOriginPolicy.NormalizeOrigin(claimedOrigin);
        return claimed is not null &&
            string.Equals(actual, claimed, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSameOrigin(Uri? left, Uri? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        var leftOrigin = CompatibilityOriginPolicy.GetOrigin(left);
        var rightOrigin = CompatibilityOriginPolicy.GetOrigin(right);
        return leftOrigin != "opaque" &&
            string.Equals(leftOrigin, rightOrigin, StringComparison.OrdinalIgnoreCase);
    }
}
