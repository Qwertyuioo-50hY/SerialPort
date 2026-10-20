using Microsoft.UI.Xaml.Media.Imaging;
using Tomography.Business.Services;
using Tomography.Business.Services.Authentication;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private void InitializeProfile(UserSession user)
    {
        AiSettingsPanel.Profile.Initialize(user, AppWindow.Id);
        void ApplyProfile(UserProfile profile)
        {
            var name = profile.DisplayName.Length > 0 ? profile.DisplayName : profile.FullName.Length > 0 ? profile.FullName : user.Username;
            var avatar = UserProfileStore.AvatarPath(user.UserId, profile);
            SideRail.SetProfile(name, avatar is null ? null : new BitmapImage(new Uri(avatar)));
            HomePanel.SetGreeting(name);
        }
        ApplyProfile(AiSettingsPanel.Profile.Profile);
        AiSettingsPanel.Profile.Saved += (_, profile) => ApplyProfile(profile);
        SideRail.ProfileRequested += (_, _) =>
        {
            ViewModel.Section = "设置"; SideRail.SetSection("设置"); AiSettingsPanel.ShowProfile();
        };
    }
}
