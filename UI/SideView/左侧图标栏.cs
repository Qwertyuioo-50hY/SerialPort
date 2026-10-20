using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Tomography.UI.SideView;

public sealed partial class IconRailView : UserControl
{
    public event EventHandler<bool>? AccountActionRequested;
    public void SetAccount(string username)
    {
        AccountPicture.DisplayName = MenuAccountPicture.DisplayName = username;
        MenuAccountName.Text = username;
        ToolTipService.SetToolTip(AccountButton, username);
    }
    private void AccountProfileClicked(object sender, RoutedEventArgs e) { AccountFlyout.Hide(); AccountActionRequested?.Invoke(this, true); }
    private void AccountSignOutClicked(object sender, RoutedEventArgs e) { AccountFlyout.Hide(); AccountActionRequested?.Invoke(this, false); }
    private void AccountThemeClicked(object sender, RoutedEventArgs e) => ToggleTheme(sender, e);
    private void AccountSettingsClicked(object sender, RoutedEventArgs e)
    {
        AccountFlyout.Hide(); NavigationRequested?.Invoke(this, "设置"); SetSection("设置");
    }
    private async void AccountHelpClicked(object sender, RoutedEventArgs e)
    {
        AccountFlyout.Hide();
        await new ContentDialog { XamlRoot = XamlRoot, RequestedTheme = ActualTheme, Title = "FieldSync 帮助",
            Content = "新建或打开项目后，可在左侧搜索和浏览文件。主页可恢复最近项目和聊天；设备连接与成像参数可在对应页面设置。", CloseButtonText = "知道了" }.ShowAsync();
    }
    public void SwitchTheme() => ToggleTheme(this, new RoutedEventArgs());
    public event EventHandler<string>? LanguageRequested;
    private void ChineseClicked(object sender, RoutedEventArgs e) => LanguageRequested?.Invoke(this, "zh");
    private void EnglishClicked(object sender, RoutedEventArgs e) => LanguageRequested?.Invoke(this, "en");
    public void SetLanguage(string language)
    {
        ChineseOption.IsChecked = language != "en"; EnglishOption.IsChecked = language == "en";
        Tomography.UI.Controls.InterfaceLanguage.Apply(this, language == "en");
    }
    public event EventHandler<string>? NavigationRequested;
    public event EventHandler? DirectoryToggleRequested;
    private void CollapseDirectory(object sender, RoutedEventArgs e) => DirectoryToggleRequested?.Invoke(this, EventArgs.Empty);
    private readonly Brush _selectedBackground;
    private readonly Brush _selectedForeground;
    private readonly Brush _defaultForeground;
    public IconRailView()
    {
        InitializeComponent();
        _selectedBackground = SelectedRailItem.Background;
        _selectedForeground = SelectedRailItem.Foreground;
        _defaultForeground = ((Button)RailItems.Children[0]).Foreground;
    }
    private void SelectRailItem(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not string section) return;
        NavigationRequested?.Invoke(this, section);
        SetSection(section);
    }
    public void SetSection(string section)
    {
        foreach (Button button in RailItems.Children.OfType<Button>().Append(AiRailItem).Append(SettingsRailItem))
        {
            var selected = button.Tag as string == section;
            button.Background = selected ? _selectedBackground : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            var color = selected ? _selectedForeground : _defaultForeground;
            button.Foreground = color;
            if (button.Content is FontIcon font) font.Foreground = color;
            else if (button.Content is Microsoft.UI.Xaml.Shapes.Path path) path.Fill = color;
        }
    }
}
