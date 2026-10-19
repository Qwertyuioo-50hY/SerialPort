using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Tomography.UI.Controls;

// Wheel gestures scroll the page; they must never silently change a selected option.
public sealed class ClickComboBox : ComboBox
{
    public ClickComboBox()
    {
        DefaultStyleKey = typeof(ComboBox);
        // The editable TextBox overlays the rounded ComboBox surface. Keep its
        // state backgrounds transparent so focus never paints square corners.
        foreach (var key in new[] { "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused" })
            Resources[key] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PopupBorder") is Microsoft.UI.Xaml.Controls.Border border)
        {
            border.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(18);
            border.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            border.Padding = new Microsoft.UI.Xaml.Thickness(6);
        }
    }
    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e) { }
}
