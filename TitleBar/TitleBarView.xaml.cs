using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace Tomography.TitleBar;

public enum TitleBarAction { Help, ToggleParameters, ToggleTools, ToggleSidebar, ToggleBottom, ToggleProject, ToggleWaveform, ToggleData, ToggleReconstruction, ToggleCamera, ZoomIn, ZoomOut, ZoomReset, ConnectDevice, OpenChat, OpenConsole, AddCommand, ConfigureApi, ConfigureAlgorithm, ConfigureWaveform, ImportFile, ExportFile, ExportMatrix, ExportImage, ExportAcquisitionCsv, ExportAcquisitionMat }

public sealed partial class TitleBarView : UserControl
{
    public event EventHandler<TitleBarAction>? ActionRequested;
    public event EventHandler? InteractiveBoundsChanged;

    public TitleBarView()
    {
        InitializeComponent();
        Loaded += (_, _) => InteractiveBoundsChanged?.Invoke(this, EventArgs.Empty);
        SizeChanged += (_, _) => InteractiveBoundsChanged?.Invoke(this, EventArgs.Empty);
        ModeSelector.SizeChanged += (_, _) => { AlignModeSelector(); InteractiveBoundsChanged?.Invoke(this, EventArgs.Empty); };
        Menus.SizeChanged += (_, _) => InteractiveBoundsChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler<string>? CommandRequested;
    public void AddCustomCommand(string name, string text)
    {
        var item = new MenuFlyoutItem { Text = name };
        item.Click += (_, _) => CommandRequested?.Invoke(this, text);
        CustomCommands.Items.Add(item);
    }

    public bool ParametersVisible { get => ParametersItem.IsChecked; set => ParametersItem.IsChecked = value; }

    public void SetInsets(double left, double right)
    {
        LeftInset.Width = new GridLength(left);
        RightInset.Width = new GridLength(right);
    }

    public void SetActive(bool active) => AppIcon.Opacity = Menus.Opacity = ModeSelector.Opacity = active ? 1 : 0.65;

    public RectInt32[] GetInteractiveBounds()
    {
        var scale = XamlRoot.RasterizationScale;
        return Menus.Visibility == Visibility.Visible
            ? new[] { Bounds(AppIcon), Bounds(Menus) } : new[] { Bounds(AppIcon), Bounds(ModeSelector) };

        RectInt32 Bounds(FrameworkElement element)
        {
            var rect = element.TransformToVisual(this).TransformBounds(new Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight));
            return new RectInt32((int)Math.Round(rect.X * scale), (int)Math.Round(rect.Y * scale),
                (int)Math.Ceiling(rect.Width * scale), (int)Math.Ceiling(rect.Height * scale));
        }
    }

    private double _modeRightEdge = 334;
    public void SetModeRightEdge(double rightEdge)
    {
        _modeRightEdge = rightEdge;
        AlignModeSelector();
    }

    private void AlignModeSelector()
    {
        if (ModeSelector.ActualWidth == 0) return;
        ModeSelector.Width = Math.Max(180, _modeRightEdge - LeftInset.ActualWidth - 84);
        ModeSelector.Margin = new Thickness(36, 0, 0, 0);
    }

    private void ViewToggleClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this,
        Enum.Parse<TitleBarAction>((string)((ToggleMenuFlyoutItem)sender).Tag));

    private int _modeIndex;
    private void CycleMode(object sender, RoutedEventArgs e)
    {
        _modeIndex = (_modeIndex + 1) % 3;
        ModeIcon.Text = new[] { "EMT", "EIT", "ECT" }[_modeIndex];
        ModeLabel.Text = new[] { "电磁层析成像", "电阻抗层析成像", "电容层析成像" }[_modeIndex];
    }

    private void ToggleMenus(object sender, RoutedEventArgs e) => SetMenusVisible(Menus.Visibility != Visibility.Visible);

    private void SetMenusVisible(bool visible)
    {
        Menus.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ModeSelector.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        InteractiveBoundsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void MenuAcceleratorInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs e)
    {
        SetMenusVisible(Menus.Visibility != Visibility.Visible);
        e.Handled = true;
    }
    private void ToggleParameters(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, TitleBarAction.ToggleParameters);
    private void ParametersAcceleratorInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs e)
    {
        ActionRequested?.Invoke(this, TitleBarAction.ToggleParameters);
        e.Handled = true;
    }
    private void MenuClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this,
        Enum.Parse<TitleBarAction>((string)((MenuFlyoutItem)sender).Tag));
}
