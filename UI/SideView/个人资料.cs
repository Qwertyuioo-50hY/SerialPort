using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using Tomography.Business.Services;
using Tomography.Business.Services.Authentication;
using Tomography.UI.Controls;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace Tomography.UI.SideView;

public sealed class ProfileSettingsView : UserControl
{
    public event EventHandler<UserProfile>? Saved;
    private UserSession? _user;
    private WindowId _owner;
    private UserProfile _profile = new();
    private bool _english;
    private readonly PersonPicture _picture = new() { Width = 100, Height = 100 };
    private readonly TextBlock _name = new() { FontSize = 26, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, FontSize = 14 };
    private readonly TextBox _fullName = Field("姓名", "ProfileFullName");
    private readonly TextBox _displayName = Field("显示名称", "ProfileDisplayName");
    private readonly TextBox _birthday = Field("生日", "ProfileBirthday", "yyyy-MM-dd");
    private readonly TextBox _country = Field("国家", "ProfileCountry");
    private readonly TextBox _region = Field("地区／省份", "ProfileRegion");
    private readonly TextBox _city = Field("城市", "ProfileCity");
    private readonly TextBox _organization = Field("机构／公司", "ProfileOrganization");
    private readonly TextBox _job = Field("职业／职位", "ProfileJobTitle");
    private readonly TextBox _website = Field("网站", "ProfileWebsite", "https://");
    private readonly TextBox _bio = Field("个人简介", "ProfileBio");
    private readonly StackPanel _body = new() { Spacing = 24, MaxWidth = 720, Margin = new Thickness(24, 24, 24, 24), HorizontalAlignment = HorizontalAlignment.Center };
    public ProfileSettingsView()
    {
        AutomationProperties.SetAutomationId(_country, "ProfileCountry");
        AutomationProperties.SetAutomationId(_status, "ProfileStatus");
        AutomationProperties.SetAutomationId(_picture, "ProfileAvatar");
        var hero = new StackPanel { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center };
        var avatar = new Button { Content = _picture, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), BorderThickness = new Thickness(0), Padding = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Center };
        avatar.Click += ChooseAvatar;
        hero.Children.Add(avatar); hero.Children.Add(_name);
        _body.Children.Add(hero);
        _body.Children.Add(new TextBlock { Text = "个人资料", FontSize = 22 });
        var grid = new Grid { ColumnSpacing = 20, RowSpacing = 20 };
        grid.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        var fields = new FrameworkElement[] { _fullName, _displayName, _birthday, _country, _region, _city, _organization, _job };
        for (var i = 0; i < fields.Length; i++)
        {
            grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
            Grid.SetRow(fields[i], i); grid.Children.Add(fields[i]);
        }
        _body.Children.Add(grid); _body.Children.Add(_website);
        _bio.MaxLength = 500; _bio.AcceptsReturn = true; _bio.TextWrapping = TextWrapping.Wrap; _bio.MinHeight = 100;
        _body.Children.Add(_bio);
        var save = Action("保存资料", "\uE74E", "SaveProfile"); save.HorizontalAlignment = HorizontalAlignment.Left;
        save.Click += async (_, _) =>
        {
            if (_user is null) return;
            save.IsEnabled = false;
            try
            {
                DateOnly? birthday = null;
                if (_birthday.Text.Trim().Length > 0)
                {
                    if (!DateOnly.TryParseExact(_birthday.Text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) throw new ArgumentException(_english ? "Use yyyy-MM-dd for the birthday." : "生日请使用 yyyy-MM-dd 格式。");
                    birthday = date;
                }
                var profile = new UserProfile { FullName = _fullName.Text.Trim(), DisplayName = _displayName.Text.Trim(), Birthday = birthday, Country = _country.Text.Trim(), Region = _region.Text.Trim(), City = _city.Text.Trim(), Organization = _organization.Text.Trim(), JobTitle = _job.Text.Trim(), Website = _website.Text.Trim(), Bio = _bio.Text.Trim(), AvatarFile = _profile.AvatarFile };
                await Task.Run(() => UserProfileStore.Save(_user.UserId, profile));
                _profile = profile; UpdateHeader(); Saved?.Invoke(this, profile);
                _status.Text = _english ? "Saved" : "已保存";
            }
            catch (Exception ex) { _status.Text = ex is ArgumentException ? InterfaceLanguage.Text(ex.Message, _english) : _english ? "Unable to save profile." : "资料保存失败，请检查文件访问权限。"; }
            finally { save.IsEnabled = true; }
        };
        _body.Children.Add(save); _body.Children.Add(_status);
        SizeChanged += (_, _) => _body.Width = Math.Max(0, Math.Min(720, ActualWidth - 48));
        Content = new ScrollViewer { HorizontalContentAlignment = HorizontalAlignment.Stretch, HorizontalScrollMode = ScrollMode.Disabled, Content = _body, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
    }
    public void Initialize(UserSession user, WindowId owner)
    {
        _user = user; _owner = owner;
        try { _profile = UserProfileStore.Load(user.UserId); }
        catch { _status.Text = "资料读取失败，原文件已保留。"; }
        _fullName.Text = _profile.FullName; _displayName.Text = _profile.DisplayName;
        _birthday.Text = _profile.Birthday?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        _country.Text = _profile.Country; _region.Text = _profile.Region; _city.Text = _profile.City;
        _organization.Text = _profile.Organization; _job.Text = _profile.JobTitle; _website.Text = _profile.Website; _bio.Text = _profile.Bio;
        var path = UserProfileStore.AvatarPath(user.UserId, _profile);
        if (path is not null) _picture.ProfilePicture = new BitmapImage(new Uri(path));
        UpdateHeader();
    }
    public void SetLanguage(bool english) { _english = english; InterfaceLanguage.Apply(_body, english); }
    public UserProfile Profile => _profile;
    private void UpdateHeader()
    {
        _picture.DisplayName = _name.Text = _profile.DisplayName.Length > 0 ? _profile.DisplayName : _profile.FullName.Length > 0 ? _profile.FullName : _user?.Username ?? "";
    }
    private async void ChooseAvatar(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender; button.IsEnabled = false;
        try
        {
            var picker = new FileOpenPicker(_owner);
            foreach (var ext in new[] { ".png", ".jpg", ".jpeg" }) picker.FileTypeFilter.Add(ext);
            var result = await picker.PickSingleFileAsync();
            if (result is null) return;
            if (new System.IO.FileInfo(result.Path).Length > 10 * 1024 * 1024) throw new ArgumentException("请选择 10 MB 以下的 PNG 或 JPEG 图片。");
            var file = await StorageFile.GetFileFromPathAsync(result.Path);
            using var stream = await file.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            if (decoder.PixelWidth > 8192 || decoder.PixelHeight > 8192) throw new ArgumentException("头像图片尺寸过大。");
            stream.Seek(0); var image = new BitmapImage(); await image.SetSourceAsync(stream);
            if (_user is null) return;
            var profile = System.Text.Json.JsonSerializer.Deserialize<UserProfile>(System.Text.Json.JsonSerializer.Serialize(_profile))!;
            await Task.Run(() => UserProfileStore.Save(_user.UserId, profile, result.Path));
            _profile = profile; _picture.ProfilePicture = image; Saved?.Invoke(this, _profile); _status.Text = "";
        }
        catch (Exception ex) { _status.Text = ex is ArgumentException ? InterfaceLanguage.Text(ex.Message, _english) : _english ? "Unable to open this image." : "图片无法读取，请选择有效的 PNG 或 JPEG 图片。"; }
        finally { button.IsEnabled = true; }
    }
    private static TextBox Field(string label, string id, string placeholder = "")
    {
        var field = new TextBox { Header = label, PlaceholderText = placeholder, MaxLength = 100, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Stretch };
        AutomationProperties.SetAutomationId(field, id); return field;
    }
    private static Button Action(string text, string glyph, string id)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new FontIcon { Glyph = glyph, FontSize = 16 }); row.Children.Add(new TextBlock { Text = text, FontSize = 16 });
        var button = new Button { Content = row, CornerRadius = new(12), Padding = new(12, 10, 12, 10) };
        AutomationProperties.SetAutomationId(button, id); return button;
    }
}
