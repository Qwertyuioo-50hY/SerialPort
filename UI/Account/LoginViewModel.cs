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
    public string FormTitle => IsRegistering ? "创建本地账号" : "登录 EMT";
    public string SubmitText => IsRegistering ? "注册账号" : "登录";
    public string SwitchText => IsRegistering ? "已有账号？返回登录" : "没有账号？注册账号";
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
        catch { Message = "本地账号或安全凭据读取失败，请检查本机存储权限后重试。"; }
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
                IsRegistering = false; ClearSecrets(); Message = "注册成功，请登录。";
            }
            else
            {
                await _authentication.LoginAsync(Username, Password, RememberAccount, AutoLogin);
                ClearSecrets(); Authenticated?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (ArgumentException ex) { Message = ex.Message; Password = Confirmation = ""; }
        catch { Message = "操作失败，请检查本地数据库和安全凭据权限后重试。"; ClearSecrets(); }
        finally { IsBusy = false; }
    }
    public void ClearSecrets() => Password = Confirmation = "";
}
