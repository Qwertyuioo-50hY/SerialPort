using System.Runtime.InteropServices;
using Tomography.Business.Services.Authentication;
using Windows.Security.Credentials;
using Windows.Storage;

namespace Tomography.UI.Account;

internal sealed class WindowsAccountStorage : IAccountStorage
{
    public WindowsAccountStorage() { }
    private const string Resource = "FieldSync.LocalLogin";
    private const string CredentialName = "AutoLogin";
#if FIELDSYNC_UNPACKAGED
    // ApplicationData.Current requires MSIX identity; standalone installations keep only
    // the same non-secret preferences in the current user's local application directory.
    private static string SettingsPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tomography", "login.json");
    public LoginPreferences Load() => System.IO.File.Exists(SettingsPath)
        ? System.Text.Json.JsonSerializer.Deserialize<LoginPreferences>(System.IO.File.ReadAllText(SettingsPath)) ?? new() : new();
    public void Save(LoginPreferences preferences)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SettingsPath)!);
        var temporary = SettingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var safe = preferences with { Username = preferences.RememberAccount ? preferences.Username : "" };
            System.IO.File.WriteAllText(temporary, System.Text.Json.JsonSerializer.Serialize(safe));
            System.IO.File.Move(temporary, SettingsPath, true);
        }
        finally { if (System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary); }
    }
#else
    private static ApplicationDataContainer Settings => ApplicationData.Current.LocalSettings;
    public LoginPreferences Load() => Settings.Values["LocalLogin"] is ApplicationDataCompositeValue value
        ? new(value["Username"] as string ?? "", value["Remember"] is true, value["AutoLogin"] is true) : new();

    public void Save(LoginPreferences preferences) => Settings.Values["LocalLogin"] = new ApplicationDataCompositeValue
    {
        ["Username"] = preferences.RememberAccount ? preferences.Username : "",
        ["Remember"] = preferences.RememberAccount, ["AutoLogin"] = preferences.AutoLogin
    };
#endif

    public string? ReadToken()
    {
        try
        {
            var credential = new PasswordVault().Retrieve(Resource, CredentialName);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x80070490)) { return null; }
    }
    public void SaveToken(string token) => new PasswordVault().Add(new PasswordCredential(Resource, CredentialName, token));
    public void DeleteToken()
    {
        var vault = new PasswordVault();
        try { vault.Remove(vault.Retrieve(Resource, CredentialName)); }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x80070490)) { }
    }
}
