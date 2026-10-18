namespace Tomography.UI.Account;

// Login presentation only; authentication rules and stored account data stay language independent.
internal static class LoginLanguage
{
    private static string SettingsPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tomography", "login-language.txt");
    public static bool LoadEnglish()
    {
        try { return System.IO.File.Exists(SettingsPath) && System.IO.File.ReadAllText(SettingsPath).Trim() == "en"; }
        catch (System.IO.IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
    public static void Save(bool english)
    {
        try
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SettingsPath)!);
            System.IO.File.WriteAllText(SettingsPath, english ? "en" : "zh");
        }
        catch (System.IO.IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    private static readonly (string Chinese, string English)[] Messages =
    [
        ("用户名须为 1–64 个字符，不能包含控制字符。", "Use 1–64 characters for your username, without control characters."),
        ("密码须为 12–128 位，并同时包含字母、数字和特殊符号。", "Use 12–128 characters for your password, including letters, numbers and symbols."),
        ("两次输入的密码不一致。", "The passwords do not match."),
        ("用户名已存在，请使用其他用户名。", "This username is taken. Choose another."),
        ("请输入用户名和密码。", "Enter your username and password."),
        ("用户名或密码错误。", "Incorrect username or password."),
        ("该账号已停用。", "This account has been disabled."),
        ("注册成功，请登录。", "Account created. Please sign in."),
        ("本地账号或安全凭据读取失败，请检查本机存储权限后重试。", "Unable to read your account. Check local storage permissions and try again."),
        ("操作失败，请检查本地数据库和安全凭据权限后重试。", "Unable to continue. Check database and credential permissions, then try again."),
        ("主窗口打开失败，请重试。", "Unable to open the main window. Please try again.")
    ];
    public static string Translate(string message, bool english)
    {
        foreach (var pair in Messages)
            if (message == pair.Chinese || message == pair.English) return english ? pair.English : pair.Chinese;
        return message;
    }
}
