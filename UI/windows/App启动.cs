using Microsoft.UI.Xaml;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Tomography.Business.Services.Authentication;
using Tomography.Data.Storage;
using Tomography.UI.Account;

namespace Tomography.UI.windows;

public partial class App : Application
{
    private Window? _window;
    private readonly ServiceProvider _services;
    private bool _transitioning;

    public App()
    {
        InitializeComponent();
        var services = new ServiceCollection();
        services.AddSingleton(_ =>
        {
            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tomography");
            return new AccountStore(new SqliteConnectionStringBuilder
            {
                DataSource = System.IO.Path.Combine(folder, "accounts.db"), ForeignKeys = true
            }.ToString());
        });
        services.AddSingleton<IAccountStorage, WindowsAccountStorage>();
        services.AddSingleton<AuthenticationService>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<AppWindow>();
        _services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        LiveCharts.Configure(settings => settings.AddSkiaSharp().AddDefaultMappers().AddDefaultTheme());
        ShowLogin(true);
    }

    private void ShowLogin(bool allowAutoLogin, bool switchAccount = false)
    {
        var login = new LoginWindow(_services.GetRequiredService<LoginViewModel>(), allowAutoLogin, switchAccount);
        _window = login;
        login.Authenticated += (_, _) =>
        {
            if (_transitioning || _window != login) return;
            _transitioning = true;
            AppWindow? main = null;
            try
            {
                main = _services.GetRequiredService<AppWindow>();
                main.SignOutRequested += SignOutRequested;
                _window = main;
                main.Activate();
                login.Close();
            }
            catch
            {
                _window = login; login.Activate(); main?.CloseForSignOut();
                login.ViewModel.Message = "主窗口打开失败，请重试。";
            }
            finally { _transitioning = false; }
        };
        login.Activate();
    }

    private async void SignOutRequested(object? sender, bool switchAccount)
    {
        if (_transitioning || sender is not AppWindow main || _window != main) return;
        _transitioning = true; main.SetAccountBusy(true);
        try
        {
            await _services.GetRequiredService<AuthenticationService>().LogoutAsync();
            ShowLogin(false, switchAccount);
            main.CloseForSignOut();
        }
        catch
        {
            if (_services.GetRequiredService<AuthenticationService>().CurrentUser is null)
            {
                main.CloseForSignOut(); Exit();
            }
            else main.ShowAccountError("退出登录失败，请检查本地数据库和安全凭据权限后重试。");
        }
        finally { if (_window == main) main.SetAccountBusy(false); _transitioning = false; }
    }
}
