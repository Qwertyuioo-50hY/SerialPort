using System.Security.Cryptography;
using System.Text;
using Tomography.Data.Storage;

namespace Tomography.Business.Services.Authentication;

public sealed record LoginPreferences(string Username = "", bool RememberAccount = false, bool AutoLogin = false);
public interface IAccountStorage
{
    LoginPreferences Load();
    void Save(LoginPreferences preferences);
    string? ReadToken();
    void SaveToken(string token);
    void DeleteToken();
}
public sealed record UserSession(long UserId, string Username);

public sealed class AuthenticationService(AccountStore accounts, IAccountStorage storage)
{
    public UserSession? CurrentUser { get; private set; }
    private string? _tokenHash;
    public LoginPreferences Preferences => storage.Load();
    private static string Normalize(string username) => username.Trim().Normalize(NormalizationForm.FormKC);
    private static string TokenHash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public Task RegisterAsync(string username, string password, string confirmation) => Task.Run(() =>
    {
        username = Normalize(username);
        if (username.Length is < 1 or > 64 || username.Any(char.IsControl))
            throw new ArgumentException("用户名须为 1–64 个字符，不能包含控制字符。");
        if (password.Length is < 8 or > 128 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new ArgumentException("密码须为 8–128 位，并同时包含字母和数字。");
        if (password != confirmation) throw new ArgumentException("两次输入的密码不一致。");
        if (!accounts.Create(username, username.ToUpperInvariant(), PasswordHasher.Hash(password)))
            throw new ArgumentException("用户名已存在，请使用其他用户名。");
    });

    public async Task LoginAsync(string username, string password, bool remember, bool autoLogin)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            throw new ArgumentException("请输入用户名和密码。");
        if (username.Length > 128 || password.Length > 128) throw new ArgumentException("用户名或密码错误。");
        var account = await Task.Run(() =>
        {
            var found = accounts.Find(Normalize(username).ToUpperInvariant());
            if (found is null || !PasswordHasher.Verify(password, found.PasswordHash))
                throw new ArgumentException("用户名或密码错误。");
            if (!found.IsEnabled) throw new ArgumentException("该账号已停用。");
            accounts.RecordLogin(found.Id);
            return found;
        });
        await ClearTokenAsync();
        if (autoLogin)
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var hash = TokenHash(token);
            await Task.Run(() => accounts.AddToken(account.Id, hash, DateTimeOffset.UtcNow.AddDays(30)));
            try { storage.SaveToken(token); _tokenHash = hash; }
            catch { await Task.Run(() => accounts.RevokeToken(hash)); throw; }
        }
        try { storage.Save(new(remember ? account.Username : "", remember, autoLogin)); }
        catch { await ClearTokenAsync(); throw; }
        CurrentUser = new(account.Id, account.Username);
    }

    public async Task<bool> TryAutoLoginAsync()
    {
        if (!Preferences.AutoLogin) return false;
        var token = storage.ReadToken();
        var hash = token is { Length: 64 } && token.All(char.IsAsciiHexDigit) ? TokenHash(token) : null;
        var account = hash is null ? null : await Task.Run(() => accounts.FindToken(hash));
        if (account is null)
        {
            await ClearTokenAsync();
            storage.Save(Preferences with { AutoLogin = false });
            return false;
        }
        await Task.Run(() => accounts.RecordLogin(account.Id));
        _tokenHash = hash;
        CurrentUser = new(account.Id, account.Username);
        return true;
    }

    private async Task ClearTokenAsync()
    {
        var token = storage.ReadToken();
        var hash = _tokenHash ?? (token is null ? null : TokenHash(token));
        if (hash is not null) await Task.Run(() => accounts.RevokeToken(hash));
        storage.DeleteToken();
        _tokenHash = null;
    }

    public async Task LogoutAsync()
    {
        // Revoke before releasing the session: a failed database write must not silently retain a usable token.
        await ClearTokenAsync();
        storage.Save(Preferences with { AutoLogin = false });
        CurrentUser = null;
    }
}
