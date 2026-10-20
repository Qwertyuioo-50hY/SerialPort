using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Tomography.UI.Controls;

public sealed class LanguagePickerFlyout : Flyout
{
    public event EventHandler<string>? LanguageSelected;
    private readonly StackPanel _panel = new() { Spacing = 4, Width = 160 };
    private readonly Dictionary<string, FontIcon> _checks = new();
    public ElementTheme Theme { get; set; } = ElementTheme.Dark;
    private readonly Border _surface = new() { CornerRadius = new(20), Padding = new(8) };
    public string Language { get; private set; } = "zh";
    public LanguagePickerFlyout()
    {
        Placement = FlyoutPlacementMode.RightEdgeAlignedBottom;
        foreach (var (language, label) in new[] { ("zh", "简体中文"), ("en", "English") })
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            var check = new FontIcon { Glyph = "\uE73E", FontSize = 13, Width = 16 }; _checks[language] = check;
            row.Children.Add(check); row.Children.Add(new TextBlock { Text = label, FontSize = 16 });
            var button = new Button { UseSystemFocusVisuals = false, Content = row, CornerRadius = new(12), Padding = new(12, 10, 12, 10), HorizontalAlignment = HorizontalAlignment.Stretch, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), BorderThickness = new(0) };
            AutomationProperties.SetAutomationId(button, language == "zh" ? "ChooseChinese" : "ChooseEnglish");
            button.Click += (_, _) => { SetLanguage(language); Hide(); LanguageSelected?.Invoke(this, language); };
            _panel.Children.Add(button);
        }
        _surface.Child = _panel;
        Content = _surface;
        FlyoutPresenterStyle = new Style(typeof(FlyoutPresenter))
        {
            Setters = {
                new Setter(FlyoutPresenter.CornerRadiusProperty, new CornerRadius(20)),
                new Setter(FlyoutPresenter.PaddingProperty, new Thickness(0)),
                new Setter(FlyoutPresenter.BorderThicknessProperty, new Thickness(0)),
                new Setter(FlyoutPresenter.MarginProperty, new Thickness(20,8,0,0)),
                new Setter(FlyoutPresenter.BackgroundProperty, new SolidColorBrush(Microsoft.UI.Colors.Transparent))
            }
        };
        Opening += (_, _) =>
        {
            _surface.RequestedTheme = Theme;
            _surface.Background = new SolidColorBrush(Theme == ElementTheme.Light ? Microsoft.UI.Colors.White : Microsoft.UI.ColorHelper.FromArgb(255,29,29,29));
            SetLanguage(Language);
        };
    }
    public void SetLanguage(string language)
    {
        Language = language;
        foreach (var pair in _checks) pair.Value.Opacity = pair.Key == language ? 1 : 0;
    }
}
