using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using Tomography.Business.Models;
using Tomography.Business.Services;

namespace Tomography.UI.SideView;

public sealed class AppearanceSettingsView : UserControl
{
    private AppearanceSettings _settings = new();
    private bool _loading;
    private WindowId _owner;
    private readonly StackPanel _body = new() { Spacing = 20, MaxWidth = 900, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(24) };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ComboBox _theme = new() { MinWidth = 150 };
    private readonly ComboBox _font = new() { MinWidth = 180, IsEditable = true };
    private readonly ComboBox _contentFont = new() { MinWidth = 180, IsEditable = true };
    private readonly ComboBox _codeFont = new() { MinWidth = 180, IsEditable = true };
    private readonly ComboBox _fontStyle = Choice("常规", "中等", "粗体");
    private readonly ComboBox _contentStyle = Choice("常规", "中等", "粗体");
    private readonly ComboBox _codeStyle = Choice("常规", "中等", "粗体");
    private readonly ComboBox _motion = Choice("系统", "开启", "关闭");
    private readonly ToggleSwitch _separate = new(), _translucent = new(), _pointer = new();
    private readonly NumberBox _size = new() { Minimum = 10, Maximum = 24, Width = 96, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact };
    private readonly NumberBox _codeSize = new() { Minimum = 10, Maximum = 24, Width = 96, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact };
    private readonly Slider _contrast = new() { Minimum = 0, Maximum = 100, Width = 180, StepFrequency = 1 };
    private readonly TextBlock _contrastValue = new() { Width = 32, VerticalAlignment = VerticalAlignment.Center };
    private readonly Dictionary<string, TextBox> _colors = new();
    private readonly List<Button> _modes = new();
    public event EventHandler<AppearanceSettings>? Changed;
    private bool Dark => _settings.Mode == "Dark" || (_settings.Mode == "Default" && ActualTheme == ElementTheme.Dark);
    private AppearancePalette Palette
    {
        get
        {
            var palette = !_settings.SeparateModes ? _settings.Shared : Dark ? _settings.Dark : _settings.Light;
            if (palette.Name is "FieldSync" or "极简" or "海蓝")
            {
                palette.Background = Dark ? "#181818" : "#F4F5F7";
                palette.Foreground = Dark ? "#FFFFFF" : "#202020";
            }
            return palette;
        }
    }
    public AppearanceSettingsView()
    {
        Content = new ScrollViewer { Content = _body, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        _body.Children.Add(new TextBlock { Text = "外观", FontSize = 30, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        _body.Children.Add(new TextBlock { Text = "视觉风格", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var modes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        foreach (var (label, mode) in new[] { ("跟随系统", "Default"), ("浅色", "Light"), ("深色", "Dark") })
        {
            var preview = new StackPanel { Spacing = 8 };
            var miniature = new Grid { Width = 64, Height = 36, Background = new SolidColorBrush(mode == "Dark" ? Colors.Black : Colors.White) };
            miniature.Children.Add(new Border { Width = 12, HorizontalAlignment = HorizontalAlignment.Left, Background = new SolidColorBrush(mode == "Light" ? Colors.LightGray : Colors.DimGray) });
            miniature.Children.Add(new Border { Height = 3, Width = 30, Background = new SolidColorBrush(Colors.DodgerBlue), Margin = new Thickness(10,0,0,0) });
            preview.Children.Add(miniature); preview.Children.Add(new TextBlock { Text = label, FontSize = 12 });
            var button = new Button { Content = preview, Tag = mode, CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(2), Padding = new Thickness(10) };
            button.Click += (_, _) => { if (_loading) return; _settings.Mode = mode;
                if (!_settings.SeparateModes && Palette.Name != "自定义")
                {
                    Palette.Background = Dark ? "#181818" : "#F4F5F7";
                    Palette.Foreground = Dark ? "#FFFFFF" : "#202020";
                }
                LoadControls(); Publish(); };
            AutomationProperties.SetName(button,label); _modes.Add(button); modes.Children.Add(button);
        }
        _body.Children.Add(Card(Row("模式", modes)));
        _theme.ItemsSource = new[] { "FieldSync", "极简", "海蓝", "自定义" };
        var themeTools = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var import = new Button { Content = "导入", CornerRadius = new CornerRadius(12) };
        var export = new Button { Content = "导出", CornerRadius = new CornerRadius(12) };
        var copy = new Button { Content = "复制", CornerRadius = new CornerRadius(12) };
        import.Click += async (_, _) => { try { var picker = new FileOpenPicker(_owner); picker.FileTypeFilter.Add(".json"); var file = await picker.PickSingleFileAsync(); if (file is null) return; SetPalette(AppearanceStore.Import(file.Path)); LoadControls(); Publish(); } catch (Exception e) { _status.Text = "主题导入失败：" + e.Message; } };
        export.Click += async (_, _) => { try { var picker = new FileSavePicker(_owner) { SuggestedFileName = "FieldSync-theme" }; picker.FileTypeChoices.Add("主题", new List<string> { ".json" }); var file = await picker.PickSaveFileAsync(); if (file is not null) AppearanceStore.Export(file.Path, Palette); } catch (Exception e) { _status.Text = "主题导出失败：" + e.Message; } };
        copy.Click += (_, _) => { var data = new Windows.ApplicationModel.DataTransfer.DataPackage(); data.SetText(System.Text.Json.JsonSerializer.Serialize(Palette, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })); Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(data); _status.Text = "主题已复制。"; };
        themeTools.Children.Add(import); themeTools.Children.Add(export); themeTools.Children.Add(copy); themeTools.Children.Add(_theme);
        var visual = new List<UIElement> { Row("主题", themeTools) };
        foreach (var label in new[] { "强调色", "背景", "前景" })
        {
            var editor = new TextBox { Width = 110, MaxLength = 7 };
            _colors[label] = editor;
            var picker = new ColorPicker { IsAlphaEnabled = false, IsHexInputVisible = true, IsColorSliderVisible = true };
            var dot = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 16, Height = 16 };
            var swatch = new Button { Content = dot, Width = 38, Height = 38, CornerRadius = new CornerRadius(19) };
            AutomationProperties.SetName(swatch,"选择" + label);
            editor.TextChanged += (_, _) => { try { dot.Fill = new SolidColorBrush((Windows.UI.Color)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color),editor.Text)); } catch { } };
            var flyout = new Flyout { Content = picker }; swatch.Flyout = flyout;
            flyout.Opening += (_, _) => { try { picker.Color = (Windows.UI.Color)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color), editor.Text); } catch { _status.Text = "请输入有效的 #RRGGBB 颜色。"; } };
            picker.ColorChanged += (_, e) => { if (_loading) return; editor.Text = $"#{e.NewColor.R:X2}{e.NewColor.G:X2}{e.NewColor.B:X2}"; ReadAndPublish(); };
            editor.LostFocus += (_, _) => ReadAndPublish();
            var group = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 }; group.Children.Add(swatch); group.Children.Add(editor);
            visual.Add(Row(label, group));
        }
        var fonts = InstalledFonts();
        _font.ItemsSource = fonts; _contentFont.ItemsSource = new[] { "与界面字体相同" }.Concat(fonts).ToArray(); _codeFont.ItemsSource = fonts;
        visual.Add(Row("字体", _font)); _body.Children.Add(Card(visual.ToArray()));
        var advanced = new StackPanel { Spacing = 20 };
        advanced.Children.Add(Card(Row("界面字号", _size), Row("代码字体大小", _codeSize)));
        advanced.Children.Add(Card(Row("减少动态效果", _motion), Row("分别设置浅色和深色模式", _separate)));
        var contentFonts = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 }; contentFonts.Children.Add(_contentFont); contentFonts.Children.Add(_contentStyle);
        var codeFonts = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 }; codeFonts.Children.Add(_codeFont); codeFonts.Children.Add(_codeStyle);
        var contrast = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 }; contrast.Children.Add(_contrast); contrast.Children.Add(_contrastValue);
        advanced.Children.Add(Card(Row("界面字体样式", _fontStyle), Row("内容字体", contentFonts), Row("代码字体", codeFonts), Row("半透明侧边栏", _translucent), Row("对比度", contrast), Row("使用指针光标", _pointer)));
        _body.Children.Add(new Expander { Header = "高级", Content = advanced, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch });
        var reset = new Button { Content = "恢复默认", CornerRadius = new CornerRadius(12) };
        reset.Click += (_, _) => { _settings = new(); LoadControls(); Publish(); };
        _body.Children.Add(reset); _body.Children.Add(_status);
        _theme.SelectionChanged += (_, _) =>
        {
            if (_loading || _theme.SelectedItem is not string name || name == "自定义") return;
            var p = new AppearancePalette { Name = name, Background = Dark ? "#181818" : "#F4F5F7", Foreground = Dark ? "#FFFFFF" : "#202020", Accent = name == "海蓝" ? "#2878E0" : name == "极简" ? "#808080" : "#18B568" };
            SetPalette(p); LoadControls(); Publish();
        };
        _motion.SelectionChanged += (_, _) => { if (_loading) return; _settings.ReduceMotion = new[] { "Default", "On", "Off" }[Math.Max(0,_motion.SelectedIndex)]; Publish(); };
        _separate.Toggled += (_, _) => { if (_loading) return; _settings.SeparateModes = _separate.IsOn; LoadControls(); Publish(); };
        _pointer.Toggled += (_, _) => { if (_loading) return; _settings.PointerCursor = _pointer.IsOn; Publish(); };
        _translucent.Toggled += (_, _) => ReadAndPublish();
        foreach (var combo in new[] { _font, _contentFont, _codeFont, _fontStyle, _contentStyle, _codeStyle }) { combo.SelectionChanged += (_, _) => ReadAndPublish(); combo.LostFocus += (_, _) => ReadAndPublish(); }
        _size.ValueChanged += (_, _) => ReadAndPublish(); _codeSize.ValueChanged += (_, _) => ReadAndPublish(); _contrast.ValueChanged += (_, _) => ReadAndPublish();
        ActualThemeChanged += (_, _) => { if (_settings.Mode == "Default") LoadControls(); };
    }
    private static string[] InstalledFonts()
    {
        var names = new HashSet<string> { "Segoe UI Variable Text", "Cascadia Mono", "Microsoft YaHei UI", "Consolas" };
        foreach (var hive in new[] { Microsoft.Win32.Registry.LocalMachine, Microsoft.Win32.Registry.CurrentUser })
        {
            using var fonts = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts");
            if (fonts is null) continue;
            foreach (var name in fonts.GetValueNames()) names.Add(System.Text.RegularExpressions.Regex.Replace(name, @"\s*\([^)]*\)$", ""));
        }
        return names.OrderBy(n => n).ToArray();
    }
    private static Brush ResourceBrush(string key) => Application.Current.Resources.TryGetValue(key,out var value) && value is Brush brush ? brush : new SolidColorBrush(Colors.Gray);
    private static ComboBox Choice(params string[] items) => new() { ItemsSource = items, MinWidth = 100 };
    private static Grid Row(string label, UIElement control)
    {
        AutomationProperties.SetName((DependencyObject)control,label);
        var grid = new Grid { Padding = new Thickness(0,12,0,12), ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1,GridUnitType.Star) }); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        Grid.SetColumn((FrameworkElement)control,1); grid.Children.Add(control);
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.SizeChanged += (_, e) =>
        {
            var narrow = e.NewSize.Width < 440;
            Grid.SetColumn((FrameworkElement)control,narrow ? 0 : 1);
            Grid.SetRow((FrameworkElement)control,narrow ? 1 : 0);
            Grid.SetColumnSpan((FrameworkElement)control,narrow ? 2 : 1);
            if (control is StackPanel panel) panel.Orientation = e.NewSize.Width < 280 ? Orientation.Vertical : Orientation.Horizontal;
        };
        return grid;
    }
    private static Border Card(params UIElement[] rows)
    {
        var stack = new StackPanel();
        foreach (var row in rows) { if (stack.Children.Count > 0) { var line = new Border { Height = 1, Opacity = 0.3 }; line.Background = ResourceBrush("DeviceDividerBrush"); stack.Children.Add(line); } stack.Children.Add(row); }
        var card = new Border { CornerRadius = new CornerRadius(20), Padding = new Thickness(20,4,20,4), BorderThickness = new Thickness(1), Child = stack };
        card.Background = ResourceBrush("ControlFillColorSecondaryBrush"); card.BorderBrush = ResourceBrush("DeviceDividerBrush"); return card;
    }
    public void LoadSettings(AppearanceSettings settings, WindowId owner) { _owner = owner; _settings = AppearanceStore.Copy(settings); LoadControls(); }
    private void SetPalette(AppearancePalette palette) { if (!_settings.SeparateModes) _settings.Shared = palette; else if (Dark) _settings.Dark = palette; else _settings.Light = palette; }
    private void LoadControls()
    {
        _loading = true;
        try
        {
            var p = Palette;
            _theme.SelectedItem = p.Name; if (_theme.SelectedItem is null) _theme.SelectedItem = "自定义";
            _colors["强调色"].Text = p.Accent; _colors["背景"].Text = p.Background; _colors["前景"].Text = p.Foreground;
            _font.Text = p.Font; _contentFont.Text = string.IsNullOrEmpty(p.ContentFont) ? "与界面字体相同" : p.ContentFont; _codeFont.Text = p.CodeFont;
            _fontStyle.SelectedItem = p.FontStyle; _contentStyle.SelectedItem = p.ContentStyle; _codeStyle.SelectedItem = p.CodeStyle;
            _size.Value = p.FontSize; _codeSize.Value = p.CodeSize; _contrast.Value = p.Contrast; _contrastValue.Text = p.Contrast.ToString("0");
            _motion.SelectedIndex = Array.IndexOf(new[] { "Default", "On", "Off" }, _settings.ReduceMotion);
            _separate.IsOn = _settings.SeparateModes; _translucent.IsOn = p.TranslucentSidebar; _pointer.IsOn = _settings.PointerCursor;
            foreach (var button in _modes) button.BorderBrush = new SolidColorBrush(button.Tag as string == _settings.Mode ? Colors.DodgerBlue : Colors.Transparent);
        }
        finally { _loading = false; }
    }
    private void ReadAndPublish()
    {
        if (_loading) return;
        var p = AppearanceStore.Copy(Palette);
        p.Name = "自定义"; p.Accent = _colors["强调色"].Text; p.Background = _colors["背景"].Text; p.Foreground = _colors["前景"].Text;
        p.Font = _font.Text; p.ContentFont = _contentFont.Text == "与界面字体相同" ? "" : _contentFont.Text; p.CodeFont = _codeFont.Text;
        p.FontStyle = _fontStyle.SelectedItem as string ?? "常规"; p.ContentStyle = _contentStyle.SelectedItem as string ?? "常规"; p.CodeStyle = _codeStyle.SelectedItem as string ?? "常规";
        p.FontSize = _size.Value; p.CodeSize = _codeSize.Value; p.Contrast = _contrast.Value; p.TranslucentSidebar = _translucent.IsOn;
        try { AppearanceStore.Validate(p); SetPalette(p); _loading = true; _theme.SelectedItem = "自定义"; _loading = false; _contrastValue.Text = p.Contrast.ToString("0"); Publish(); } catch (ArgumentException e) { _status.Text = e.Message; }
    }
    private void Publish()
    {
        try { AppearanceStore.Validate(_settings); Changed?.Invoke(this, AppearanceStore.Copy(_settings)); _status.Text = "已保存"; }
        catch (Exception e) { _status.Text = "外观保存失败：" + e.Message; }
    }
}
