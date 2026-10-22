using System.Text.Json;
using System.Text.RegularExpressions;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

public static class AppearanceStore
{
    public static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    public static void Validate(AppearanceSettings settings)
    {
        if (settings.Mode is not ("Default" or "Light" or "Dark") || settings.ReduceMotion is not ("Default" or "On" or "Off"))
            throw new ArgumentException("外观模式无效。");
        foreach (var palette in new[] { settings.Shared, settings.Light, settings.Dark }) Validate(palette);
    }
    public static void Validate(AppearancePalette palette)
    {
        if (palette is null || new[] { palette.Accent, palette.Background, palette.Foreground }.Any(c => c is null || !Regex.IsMatch(c, "^#[0-9a-fA-F]{6}$")))
            throw new ArgumentException("颜色须为 #RRGGBB 格式。");
        if (!double.IsFinite(palette.FontSize) || palette.FontSize is < 10 or > 24 ||
            !double.IsFinite(palette.CodeSize) || palette.CodeSize is < 10 or > 24 ||
            !double.IsFinite(palette.Contrast) || palette.Contrast is < 0 or > 100)
            throw new ArgumentException("字号应为 10–24，对比度应为 0–100。");
        if (string.IsNullOrWhiteSpace(palette.Font) || string.IsNullOrWhiteSpace(palette.CodeFont) || palette.ContentFont is null ||
            new[] { palette.FontStyle, palette.ContentStyle, palette.CodeStyle }.Any(s => s is not ("常规" or "中等" or "粗体")))
            throw new ArgumentException("字体配置无效。");
    }
    public static AppearancePalette Import(string path)
    {
        var palette = JsonSerializer.Deserialize<AppearancePalette>(File.ReadAllText(path)) ?? throw new ArgumentException("主题文件为空。");
        Validate(palette);
        return palette;
    }
    public static void Export(string path, AppearancePalette palette)
    {
        Validate(palette);
        File.WriteAllText(path, JsonSerializer.Serialize(palette, new JsonSerializerOptions { WriteIndented = true }));
    }
}
