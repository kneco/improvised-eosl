namespace ImprovisedEosl.Core;

public enum StartupNavigationSource
{
    AutomaticValidation,
    SelectedProfile,
    UserSettings,
    BuiltInHome
}

public sealed record StartupNavigationDecision(Uri Uri, StartupNavigationSource Source);

public static class StartupNavigationPolicy
{
    public static StartupNavigationDecision Resolve(
        Uri? automaticValidationUri,
        CompatibilityProfile? selectedProfile,
        BrowserSettings userSettings,
        Uri builtInHomeUri)
    {
        ArgumentNullException.ThrowIfNull(userSettings);
        ArgumentNullException.ThrowIfNull(builtInHomeUri);

        if (automaticValidationUri is not null)
        {
            return new(automaticValidationUri, StartupNavigationSource.AutomaticValidation);
        }
        if (selectedProfile is not null)
        {
            return new(selectedProfile.StartUrl, StartupNavigationSource.SelectedProfile);
        }
        if (userSettings.InitialUrl is not null)
        {
            return new(userSettings.InitialUrl, StartupNavigationSource.UserSettings);
        }
        return new(builtInHomeUri, StartupNavigationSource.BuiltInHome);
    }
}
