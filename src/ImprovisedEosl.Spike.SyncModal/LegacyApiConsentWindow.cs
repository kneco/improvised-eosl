using System.Windows;
using System.Windows.Controls;
using ImprovisedEosl.Core;

namespace ImprovisedEosl.Spike.SyncModal;

public enum LegacyApiConsentChoice
{
    AllowSelected,
    AllowAll,
    DenyAll
}

public sealed class LegacyApiConsentWindow : Window
{
    private readonly Dictionary<string, CheckBox> _apiChecks = [];

    public LegacyApiConsentWindow(
        string origin,
        string detectedApiName,
        IReadOnlySet<string> currentlyAllowedApis)
    {
        Title = UiText.Get(UiText.ConsentTitle);
        Width = 620;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var root = new StackPanel { Margin = new Thickness(18) };
        root.Children.Add(new TextBlock
        {
            Text = UiText.Format(UiText.ConsentBody, detectedApiName, origin),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });

        foreach (var apiName in CompatibilityApi.Known)
        {
            var check = new CheckBox
            {
                Content = apiName,
                IsChecked = apiName == detectedApiName || currentlyAllowedApis.Contains(apiName),
                Margin = new Thickness(8, 4, 0, 4)
            };
            _apiChecks.Add(apiName, check);
            root.Children.Add(check);
        }

        root.Children.Add(new TextBlock
        {
            Text = UiText.Get(UiText.ConsentKnownApisOnly),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 8)
        });
        root.Children.Add(new TextBlock
        {
            Text = UiText.Get(UiText.ConsentReloadNote),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 18)
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(CreateButton(UiText.ConsentDenyAll, LegacyApiConsentChoice.DenyAll));
        buttons.Children.Add(CreateButton(UiText.ConsentAllowSelected, LegacyApiConsentChoice.AllowSelected));
        buttons.Children.Add(CreateButton(UiText.ConsentAllowAll, LegacyApiConsentChoice.AllowAll, isDefault: true));
        root.Children.Add(buttons);
        Content = root;
    }

    public LegacyApiConsentChoice? Choice { get; private set; }

    public IReadOnlySet<string> SelectedApis => _apiChecks
        .Where(item => item.Value.IsChecked == true)
        .Select(item => item.Key)
        .ToHashSet(StringComparer.Ordinal);

    private Button CreateButton(string textKey, LegacyApiConsentChoice choice, bool isDefault = false)
    {
        var button = new Button
        {
            Content = UiText.Get(textKey),
            MinWidth = 120,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(10, 4, 10, 4),
            IsDefault = isDefault
        };
        button.Click += (_, _) =>
        {
            Choice = choice;
            DialogResult = true;
        };
        return button;
    }
}
