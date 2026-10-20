using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Tomography.UI.Controls;

internal static class RoundedProjectPrompt
{
    public static Task<string?> ShowAsync(FrameworkElement owner, bool english)
    {
        var completion = new TaskCompletionSource<string?>();
        var field = new TextBox { Header = english ? "Project name" : "项目名称", PlaceholderText = english ? "Enter a project name" : "输入项目名称", MaxLength = 100, FontSize = 16 };
        AutomationProperties.SetAutomationId(field, "ProjectName");
        var content = new StackPanel { Width = 320, Spacing = 20, RequestedTheme = owner.ActualTheme };
        content.Children.Add(new TextBlock { Text = english ? "New project" : "新建项目", FontSize = 24, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        content.Children.Add(field);
        var actions = new Grid { ColumnSpacing = 12 };
        actions.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) }); actions.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        var primary = new Button { Content = english ? "Choose location" : "选择保存位置", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, CornerRadius = new(14), Padding = new(12), BorderThickness = new(0), Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255,25,182,87)), Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) };
        var cancel = new Button { Content = english ? "Cancel" : "取消", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, CornerRadius = new(14), Padding = new(12), BorderThickness = new(0) };
        AutomationProperties.SetAutomationId(primary, "ProjectChooseLocation"); AutomationProperties.SetAutomationId(cancel, "ProjectCancel");
        actions.Children.Add(primary); Grid.SetColumn(cancel, 1); actions.Children.Add(cancel); content.Children.Add(actions);
        var flyout = new Flyout { Content = content, Placement = FlyoutPlacementMode.Full, FlyoutPresenterStyle = new Style(typeof(FlyoutPresenter))
        {
            Setters = {
                new Setter(FlyoutPresenter.VerticalAlignmentProperty, VerticalAlignment.Center),
                new Setter(FlyoutPresenter.MaxHeightProperty, 280d),
                new Setter(FlyoutPresenter.CornerRadiusProperty, new CornerRadius(24)),
                new Setter(FlyoutPresenter.BorderThicknessProperty, new Thickness(0)),
                new Setter(FlyoutPresenter.PaddingProperty, new Thickness(24)),
                new Setter(FlyoutPresenter.RequestedThemeProperty, owner.ActualTheme),
                new Setter(FlyoutPresenter.BackgroundProperty, new SolidColorBrush(owner.ActualTheme == ElementTheme.Light ? Microsoft.UI.Colors.White : Microsoft.UI.ColorHelper.FromArgb(255,29,29,29)))
            }
        } };
        primary.Click += (_, _) => { completion.TrySetResult(field.Text); flyout.Hide(); };
        cancel.Click += (_, _) => flyout.Hide();
        flyout.Closed += (_, _) => completion.TrySetResult(null);
        flyout.Opened += (_, _) => field.Focus(FocusState.Programmatic);
        flyout.ShowAt(owner);
        return completion.Task;
    }
}
