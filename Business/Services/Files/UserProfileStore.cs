using System.Text.Json;

namespace Tomography.Business.Services;

public sealed class UserProfile
{
    public string FullName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateOnly? Birthday { get; set; }
    public string Country { get; set; } = "";
    public string Region { get; set; } = "";
    public string City { get; set; } = "";
    public string Organization { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string Bio { get; set; } = "";
    public string Website { get; set; } = "";
    public string AvatarFile { get; set; } = "";
}

public static class UserProfileStore
{
    private static string Folder(long userId)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tomography", "Profiles", userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
    public static UserProfile Load(long userId)
    {
        var path = Path.Combine(Folder(userId), "profile.json");
        return File.Exists(path) ? JsonSerializer.Deserialize<UserProfile>(File.ReadAllText(path)) ?? new() : new();
    }
    public static string? AvatarPath(long userId, UserProfile profile)
    {
        var name = profile.AvatarFile;
        if (string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name) return null;
        var path = Path.Combine(Folder(userId), name);
        return File.Exists(path) ? path : null;
    }
    public static void Save(long userId, UserProfile profile, string? avatarSource = null)
    {
        foreach (var value in new[] { profile.FullName, profile.DisplayName, profile.Country, profile.Region, profile.City, profile.Organization, profile.JobTitle })
            if (value.Length > 100 || value.Any(char.IsControl)) throw new ArgumentException("资料字段最多 100 个字符，不能包含控制字符。");
        if (profile.Bio.Length > 500) throw new ArgumentException("个人简介最多 500 个字符。");
        if (profile.Birthday > DateOnly.FromDateTime(DateTime.Today)) throw new ArgumentException("生日不能晚于今天。");
        if (profile.Website.Length > 0 && (!Uri.TryCreate(profile.Website, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || uri.Host.Length == 0))
            throw new ArgumentException("网站须为有效的 HTTP 或 HTTPS 地址。");
        if (profile.AvatarFile.Length > 0 && Path.GetFileName(profile.AvatarFile) != profile.AvatarFile) throw new ArgumentException("头像文件名无效。");
        var folder = Folder(userId);
        string? createdAvatar = null;
        var old = Load(userId).AvatarFile;
        Directory.CreateDirectory(folder);
        var temporary = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            if (avatarSource is not null)
            {
                var ext = Path.GetExtension(avatarSource).ToLowerInvariant();
                if (ext is not (".png" or ".jpg" or ".jpeg") || new FileInfo(avatarSource).Length > 10 * 1024 * 1024) throw new ArgumentException("请选择 10 MB 以下的 PNG 或 JPEG 图片。");
                profile.AvatarFile = Guid.NewGuid().ToString("N") + ext;
                createdAvatar = Path.Combine(folder, profile.AvatarFile);
                File.Copy(avatarSource, createdAvatar);
            }
            File.WriteAllText(temporary, JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, Path.Combine(folder, "profile.json"), true);
        }
        catch { if (createdAvatar is not null && File.Exists(createdAvatar)) File.Delete(createdAvatar); throw; }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        if (old.Length > 0 && old != profile.AvatarFile && Path.GetFileName(old) == old)
            try { File.Delete(Path.Combine(folder, old)); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
