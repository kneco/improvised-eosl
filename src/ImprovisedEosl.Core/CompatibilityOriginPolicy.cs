namespace ImprovisedEosl.Core;

public sealed class CompatibilityOriginPolicy
{
    private readonly HashSet<string> _configuredShowModalDialogOrigins = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _mutableShowModalDialogOrigins = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _mutableWindowOpenFeatureOrigins = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _mutableTopLevelCloseHandoffOrigins = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<UserApprovedCompatibility> _denials = [];
    private LegacyApiDetection? _pendingDetection;

    public CompatibilityOriginPolicy()
    {
    }

    public CompatibilityOriginPolicy(
        IEnumerable<UserApprovedCompatibility> approvals,
        IEnumerable<ConfiguredCompatibility>? configuredCompatibility = null,
        IEnumerable<UserApprovedCompatibility>? denials = null)
    {
        foreach (var approval in approvals)
        {
            Allow(approval.Origin, approval.ApiName);
        }

        foreach (var denial in denials ?? Array.Empty<UserApprovedCompatibility>())
        {
            Deny(denial.Origin, denial.ApiName);
        }

        foreach (var configured in configuredCompatibility ?? Array.Empty<ConfiguredCompatibility>())
        {
            var normalizedOrigin = NormalizeOrigin(configured.Origin);
            if (configured.ApiName == CompatibilityApi.ShowModalDialog && normalizedOrigin is not null)
            {
                _configuredShowModalDialogOrigins.Add(normalizedOrigin);
            }
        }
    }

    public LegacyApiDetection? PendingDetection => _pendingDetection;

    public bool Allow(string origin, string apiName)
    {
        var normalizedOrigin = NormalizeOrigin(origin);
        if (normalizedOrigin is not null && CompatibilityApi.IsKnown(apiName))
        {
            _denials.Remove(new UserApprovedCompatibility(normalizedOrigin, apiName));
            return apiName switch
            {
                CompatibilityApi.ShowModalDialog => _mutableShowModalDialogOrigins.Add(normalizedOrigin),
                CompatibilityApi.WindowOpenFeatures => _mutableWindowOpenFeatureOrigins.Add(normalizedOrigin),
                CompatibilityApi.TopLevelCloseHandoff =>
                    _mutableTopLevelCloseHandoffOrigins.Add(normalizedOrigin),
                _ => false
            };
        }

        return false;
    }

    public void ClearPendingDetection()
    {
        _pendingDetection = null;
    }

    public LegacyApiDetection Detect(string origin, string apiName)
    {
        _pendingDetection = new LegacyApiDetection(origin, apiName);
        return _pendingDetection;
    }

    public bool IsAllowed(string origin, string apiName)
    {
        var normalizedOrigin = NormalizeOrigin(origin);
        if (normalizedOrigin is null)
        {
            return false;
        }

        return apiName switch
        {
            CompatibilityApi.ShowModalDialog =>
                _configuredShowModalDialogOrigins.Contains(normalizedOrigin) ||
                _mutableShowModalDialogOrigins.Contains(normalizedOrigin),
            CompatibilityApi.WindowOpenFeatures =>
                _mutableWindowOpenFeatureOrigins.Contains(normalizedOrigin),
            CompatibilityApi.TopLevelCloseHandoff =>
                _mutableTopLevelCloseHandoffOrigins.Contains(normalizedOrigin),
            _ => false
        };
    }

    public bool IsConfigured(string origin, string apiName)
    {
        var normalizedOrigin = NormalizeOrigin(origin);
        return apiName == CompatibilityApi.ShowModalDialog &&
            normalizedOrigin is not null &&
            _configuredShowModalDialogOrigins.Contains(normalizedOrigin);
    }

    public IReadOnlyList<UserApprovedCompatibility> GetApprovals()
    {
        return _mutableShowModalDialogOrigins
            .Select(origin => new UserApprovedCompatibility(origin, CompatibilityApi.ShowModalDialog))
            .Concat(_mutableWindowOpenFeatureOrigins.Select(origin =>
                new UserApprovedCompatibility(origin, CompatibilityApi.WindowOpenFeatures)))
            .Concat(_mutableTopLevelCloseHandoffOrigins.Select(origin =>
                new UserApprovedCompatibility(origin, CompatibilityApi.TopLevelCloseHandoff)))
            .OrderBy(item => item.Origin, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ApiName, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<UserApprovedCompatibility> GetDenials() =>
        _denials.OrderBy(item => item.Origin, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ApiName, StringComparer.Ordinal)
            .ToArray();

    public bool Deny(string origin, string apiName)
    {
        var normalizedOrigin = NormalizeOrigin(origin);
        if (normalizedOrigin is null || !CompatibilityApi.IsKnown(apiName))
        {
            return false;
        }

        Revoke(normalizedOrigin, apiName);
        return _denials.Add(new UserApprovedCompatibility(normalizedOrigin, apiName));
    }

    public bool IsDenied(string origin, string apiName)
    {
        var normalizedOrigin = NormalizeOrigin(origin);
        return normalizedOrigin is not null &&
            _denials.Contains(new UserApprovedCompatibility(normalizedOrigin, apiName));
    }

    public bool ClearDecision(string origin, string apiName)
    {
        var normalizedOrigin = NormalizeOrigin(origin);
        return normalizedOrigin is not null &&
            (Revoke(normalizedOrigin, apiName) |
             _denials.Remove(new UserApprovedCompatibility(normalizedOrigin, apiName)));
    }

    public bool Revoke(string origin, string apiName)
    {
        var normalizedOrigin = NormalizeOrigin(origin);
        if (normalizedOrigin is null)
        {
            return false;
        }

        return apiName switch
        {
            CompatibilityApi.ShowModalDialog => _mutableShowModalDialogOrigins.Remove(normalizedOrigin),
            CompatibilityApi.WindowOpenFeatures => _mutableWindowOpenFeatureOrigins.Remove(normalizedOrigin),
            CompatibilityApi.TopLevelCloseHandoff =>
                _mutableTopLevelCloseHandoffOrigins.Remove(normalizedOrigin),
            _ => false
        };
    }

    public CompatibilityStatus GetStatus(Uri uri)
    {
        var origin = GetOrigin(uri);
        if (_pendingDetection is not null &&
            string.Equals(_pendingDetection.Origin, origin, StringComparison.OrdinalIgnoreCase))
        {
            return new CompatibilityStatus(
                origin,
                "Compatibility: legacy API detected; permission needed");
        }

        var showModalDialog = IsAllowed(origin, CompatibilityApi.ShowModalDialog);
        var windowOpenFeatures = IsAllowed(origin, CompatibilityApi.WindowOpenFeatures);
        var topLevelCloseHandoff = IsAllowed(origin, CompatibilityApi.TopLevelCloseHandoff);
        var enabledCount = (showModalDialog ? 1 : 0) +
            (windowOpenFeatures ? 1 : 0) +
            (topLevelCloseHandoff ? 1 : 0);
        if (enabledCount >= 2)
        {
            return new CompatibilityStatus(origin, "Compatibility: known legacy features enabled for this origin");
        }

        if (showModalDialog)
        {
            return new CompatibilityStatus(
                origin,
                "Compatibility: showModalDialog enabled for this origin");
        }

        if (windowOpenFeatures)
        {
            return new CompatibilityStatus(origin, "Compatibility: window.open features enabled for this origin");
        }

        if (topLevelCloseHandoff)
        {
            return new CompatibilityStatus(origin, "Compatibility: window.close handoff enabled for this origin");
        }

        return origin == "opaque"
            ? new CompatibilityStatus(origin, "Compatibility: blocked for this origin")
            : new CompatibilityStatus(origin, "Compatibility: off");
    }

    public static string GetOrigin(Uri uri)
    {
        return NormalizeOrigin(uri) ?? "opaque";
    }

    public static string? NormalizeOrigin(string origin)
    {
        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            ? NormalizeOrigin(uri)
            : null;
    }

    private static string? NormalizeOrigin(Uri uri)
    {
        if (!uri.IsAbsoluteUri ||
            string.IsNullOrEmpty(uri.Host) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        var host = uri.HostNameType == UriHostNameType.IPv6
            ? $"[{uri.IdnHost}]"
            : uri.IdnHost;
        return $"{uri.Scheme.ToLowerInvariant()}://{host.ToLowerInvariant()}:{uri.Port}";
    }
}
