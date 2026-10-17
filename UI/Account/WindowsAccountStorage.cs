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
    private static ApplicationDataContainer Settings => ApplicationData.Current.LocalSettings;
    public LoginPreferences Load() => Settings.Values["LocalLogin"] is ApplicationDataCompositeValue value
        ? new(value["Username"] as string ?? "", value["Remember"] is true, value["AutoLogin"] is true) : new();

    public void Save(LoginPreferences preferences) => Settings.Values["LocalLogin"] = new ApplicationDataCompositeValue
    {
        ["Username"] = preferences.RememberAccount ? preferences.Username : "",
        ["Remember"] = preferences.RememberAccount, ["AutoLogin"] = preferences.AutoLogin
    };

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
