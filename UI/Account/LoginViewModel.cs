using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tomography.Business.Services.Authentication;

namespace Tomography.UI.Account;

public sealed class LoginViewModel : ObservableObject
{
    private readonly AuthenticationService _authentication;
    private string _username = "", _password = "", _confirmation = "", _message = "";
    private bool _rememberAccount, _autoLogin, _isRegistering, _isBusy;
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string Confirmation { get => _confirmation; set => SetProperty(ref _confirmation, value); }
    public string Message { get => _message; set => SetProperty(ref _message, value); }
    public bool RememberAccount { get => _rememberAccount; set => SetProperty(ref _rememberAccount, value); }
    public bool AutoLogin { get => _autoLogin; set => SetProperty(ref _autoLogin, value); }
    public bool IsRegistering
    {
        get => _isRegistering;
        private set
        {
            SetProperty(ref _isRegistering, value);
            OnPropertyChanged(nameof(IsLogin)); OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(SubmitText)); OnPropertyChanged(nameof(SwitchText));
        }
    }
    public bool IsLogin => !IsRegistering;
    private bool _english = LoginLanguage.LoadEnglish();
    public bool English => _english;
    private string Text(string chinese, string english) => English ? english : chinese;
    public string FormTitle => IsRegistering ? Text("注册", "Sign up") : Text("登录", "Sign in");
    public string UsernameText => Text("用户名", "Username");
    public string PasswordText => Text("密码", "Password");
    public string ConfirmationText => Text("确认密码", "Confirm password");
    public string PasswordHint => Text("12–128 位，包含字母、数字和特殊符号", "12–128 characters, including letters, numbers and symbols");
    public string RememberText => Text("记住账号", "Remember me");
    public string AutoLoginText => Text("自动登录", "Auto sign-in");
    public string AutoLoginHint => Text("使用有效期 30 天的安全令牌，不保存密码", "Uses a secure 30-day token. Your password is not saved.");
    public string ShowPasswordText => Text("显示密码", "Show password");
    public string HidePasswordText => Text("隐藏密码", "Hide password");
    public string MatchText => Text("密码一致", "Passwords match");
    public string MismatchText => Text("密码不一致", "Passwords do not match");
    public string ThemeText => Text("主题", "Appearance");
    public string DarkText => Text("黑色", "Dark");
    public string LightText => Text("白色", "Light");
    public string LanguageText => Text("语言", "Language");
    public string MinimizeText => Text("最小化", "Minimize");
    public string CloseText => Text("关闭", "Close");
    public void SetLanguage(bool english)
    {
        _english = english;
        LoginLanguage.Save(english);
        Message = LoginLanguage.Translate(Message, english);
        OnPropertyChanged(string.Empty);
    }
    public string SubmitText => IsRegistering ? Text("注册", "Create account") : Text("登录", "Sign in");
    public string SwitchText => IsRegistering ? Text("已有账号？登录", "Already have an account? Sign in") : Text("创建账号", "Create an account");
    public bool IsBusy
    {
        get => _isBusy;
        private set { SetProperty(ref _isBusy, value); OnPropertyChanged(nameof(IsIdle)); }
    }
    public bool IsIdle => !IsBusy;
    public IAsyncRelayCommand SubmitCommand { get; }
    public IRelayCommand SwitchModeCommand { get; }
    public event EventHandler? Authenticated;

    public LoginViewModel(AuthenticationService authentication)
    {
        _authentication = authentication;
        SubmitCommand = new AsyncRelayCommand(SubmitAsync);
        SwitchModeCommand = new RelayCommand(() =>
        {
            if (IsBusy) return;
            IsRegistering = !IsRegistering; ClearSecrets(); Message = "";
        });
    }

    public async Task InitializeAsync(bool allowAutoLogin)
    {
        IsBusy = true;
        try
        {
            var preferences = _authentication.Preferences;
            Username = preferences.RememberAccount ? preferences.Username : "";
            RememberAccount = preferences.RememberAccount;
            // Automatic sign-in is never preselected on the form; the user explicitly opts in.
            AutoLogin = false;
            if (allowAutoLogin && await _authentication.TryAutoLoginAsync()) Authenticated?.Invoke(this, EventArgs.Empty);
        }
        catch { Message = Text("本地账号或安全凭据读取失败，请检查本机存储权限后重试。", "Unable to read your account. Check local storage permissions and try again."); }
        finally { IsBusy = false; }
    }

    private async Task SubmitAsync()
    {
        if (IsBusy) return;
        IsBusy = true; Message = "";
        try
        {
            if (IsRegistering)
            {
                await _authentication.RegisterAsync(Username, Password, Confirmation);
                IsRegistering = false; ClearSecrets(); Message = Text("注册成功，请登录。", "Account created. Please sign in.");
            }
            else
            {
                await _authentication.LoginAsync(Username, Password, RememberAccount, AutoLogin);
                ClearSecrets(); Authenticated?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (ArgumentException ex) { Message = LoginLanguage.Translate(ex.Message, English); Password = Confirmation = ""; }
        catch { Message = Text("操作失败，请检查本地数据库和安全凭据权限后重试。", "Unable to continue. Check database and credential permissions, then try again."); ClearSecrets(); }
        finally { IsBusy = false; }
    }
    public void ClearSecrets() => Password = Confirmation = "";
}
