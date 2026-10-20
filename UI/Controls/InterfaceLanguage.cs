using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Tomography.UI.Controls;

public static class InterfaceLanguage
{
    private static readonly (string Zh, string En)[] Labels =
    [
        ("个人资料", "Profile"), ("姓名", "Full name"), ("显示名称", "Display name"), ("生日", "Birthday"),
        ("国家", "Country"), ("地区／省份", "Region / Province"), ("城市", "City"), ("机构／公司", "Organization"),
        ("职业／职位", "Job title"), ("网站", "Website"), ("个人简介", "About me"), ("更换头像", "Change photo"),
        ("移除头像", "Remove photo"), ("保存资料", "Save profile"),
        ("资料均为可选，保存在本机；显示名称不会改变登录账号。", "Optional details, stored on this device. Your display name does not change your login."),
        ("资料字段最多 100 个字符，不能包含控制字符。", "Use up to 100 characters per field, without control characters."),
        ("个人简介最多 500 个字符。", "Use up to 500 characters for your bio."),
        ("生日不能晚于今天。", "Your birthday cannot be in the future."),
        ("网站须为有效的 HTTP 或 HTTPS 地址。", "Enter a valid HTTP or HTTPS website URL."),
        ("请选择 10 MB 以下的 PNG 或 JPEG 图片。", "Choose a PNG or JPEG image smaller than 10 MB."),
        ("主页", "Home"), ("设备", "Device"), ("数据分析", "Analysis"), ("项目导航", "Project navigation"),
        ("新建项目", "New project"), ("打开项目", "Open project"), ("打开文件夹", "Open folder"),
        ("最近项目", "Recent projects"), ("最近聊天", "Recent chats"), ("搜索项目文件", "Search project files"),
        ("折叠资源栏", "Toggle project navigation"), ("AI 聊天", "AI chat"), ("主题", "Appearance"),
        ("设置", "Settings"), ("帮助", "Help"), ("退出登录", "Sign out"), ("切换主题", "Toggle theme"),
        ("账号", "Account"), ("语言", "Language"), ("深色模式", "Dark mode"),
        ("搜索设置", "Search settings"), ("通用", "General"), ("外观", "Appearance"),
        ("快捷键", "Keyboard shortcuts"), ("语言和地区", "Language and region"), ("暂无可配置项", "No settings available"),
        ("未找到设置分类", "No settings found"), ("由 CC Switch 管理模型配置", "Use CC Switch configuration"),
        ("CC Switch / Codex config.toml 路径", "CC Switch / Codex config.toml path"),
        ("启用后，每次发送读取 TOML 中的模型、地址、协议及认证参数；下方对应参数仅在手动配置时生效。", "When enabled, requests use the model, URL, protocol and credentials from TOML."),
        ("API 地址（Base URL，Anthropic 可填主机根地址）", "API base URL"),
        ("API Key 环境变量（未填写密钥时使用）", "API key environment variable"),
        ("模型选择（可输入中转站模型名）", "Model"), ("可选模型（每行一个）", "Available models (one per line)"),
        ("接口协议", "API protocol"), ("模型模式", "Model mode"), ("思考强度", "Reasoning effort"),
        ("模型速度", "Service tier"), ("输出详细程度", "Response detail"), ("输出格式", "Output format"),
        ("语气", "Tone"), ("输出语言", "Response language"), ("自定义指令", "Custom instructions"),
        ("输出内容要求", "Response instructions"), ("输出 Token 上限（0 为模型默认）", "Maximum output tokens (0 = default)"),
        ("请求超时（秒）", "Request timeout (seconds)"), ("保存", "Save"), ("已保存", "Saved"),
        ("文本", "Text"), ("模型默认", "Model default"), ("标准 (default)", "Standard (default)"),
        ("快速 (Fast)", "Fast"), ("自动 (auto)", "Auto"), ("新建对话", "New chat"),
        ("聊天消息", "Message"), ("发送消息", "Send message"), ("模型和思考设置", "Model and reasoning"),
        ("键盘快捷键", "Keyboard shortcuts"), ("全部重置为默认值", "Reset all to defaults"),
        ("搜索快捷键", "Search shortcuts"), ("未设置", "Not set"),
        ("最小化", "Minimize"), ("最大化或还原", "Maximize or restore"), ("关闭", "Close"),
        ("连接", "Connect"), ("断开连接", "Disconnect"), ("握手", "Handshake"),
        ("端口", "Port"), ("波特率", "Baud rate"), ("数据位", "Data bits"), ("校验位", "Parity"),
        ("停止位", "Stop bits"), ("流控", "Flow control"), ("编码", "Encoding"),
        ("重建算法", "Reconstruction algorithm"), ("差分模式", "Difference mode"),
        ("迭代次数", "Iterations"), ("TV 权重", "TV weight"), ("选择自定义 Python 算法", "Choose custom Python algorithm"),
        ("空场采集", "Acquire reference"), ("单次采集", "Acquire single frame"), ("多次采集", "Acquire batch"), ("实时采集", "Live acquisition")
    ];
    public static string Text(string value, bool english)
    {
        foreach (var pair in Labels) if (value == pair.Zh || value == pair.En) return english ? pair.En : pair.Zh;
        return value;
    }
    public static void Apply(FrameworkElement root, bool english)
    {
        root.Language = english ? "en-US" : "zh-CN";
        Visit(root, english);
    }
    private static void Visit(DependencyObject node, bool english)
    {
        // User content, file names and conversation messages must never be translated.
        if (node is TreeView || node is ListView list && list.Name != "SettingsCategories") return;
        if (node is TextBlock text && text.Name != "MenuAccountName") text.Text = Text(text.Text, english);
        if (node is ContentControl content && content.Content is string label) content.Content = Text(label, english);
        switch (node)
        {
            case TextBox box: box.PlaceholderText = Text(box.PlaceholderText, english); if (box.Header is string textHeader) box.Header = Text(textHeader, english); break;
            case PasswordBox box: box.PlaceholderText = Text(box.PlaceholderText, english); if (box.Header is string passwordHeader) box.Header = Text(passwordHeader, english); break;
            case NumberBox box when box.Header is string numberHeader: box.Header = Text(numberHeader, english); break;
            case ComboBox box:
                if (box.Header is string comboHeader) box.Header = Text(comboHeader, english);
                foreach (var item in box.Items.OfType<ComboBoxItem>()) if (item.Content is string v) item.Content = Text(v, english);
                break;
            case AutoSuggestBox box: box.PlaceholderText = Text(box.PlaceholderText, english); break;
            case ToggleSwitch toggle when toggle.Header is string toggleHeader: toggle.Header = Text(toggleHeader, english); break;
        }
        if (node is FrameworkElement element)
        {
            var name = AutomationProperties.GetName(element);
            if (!string.IsNullOrEmpty(name)) AutomationProperties.SetName(element, Text(name, english));
            if (ToolTipService.GetToolTip(element) is string tip) ToolTipService.SetToolTip(element, Text(tip, english));
        }
        if (node is Button button)
        {
            if (button.Flyout is Flyout flyout && flyout.Content is FrameworkElement contentRoot) Visit(contentRoot, english);
            if (button.Flyout is MenuFlyout menu)
                foreach (var item in menu.Items.OfType<MenuFlyoutItem>()) item.Text = Text(item.Text, english);
        }
        if (node is TextBox or PasswordBox or NumberBox or ComboBox or AutoSuggestBox) return;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) Visit(VisualTreeHelper.GetChild(node, i), english);
    }
}
