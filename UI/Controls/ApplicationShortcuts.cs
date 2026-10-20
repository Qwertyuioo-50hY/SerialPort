using Windows.System;

namespace Tomography.UI.Controls;

public static class ApplicationShortcuts
{
    public static readonly (string Id, string Chinese, string English, string Default)[] Actions =
    [
        ("NewChat", "新聊天", "New chat", "Ctrl+N"),
        ("NewProject", "新建项目", "New project", "Ctrl+Shift+N"),
        ("OpenProject", "打开项目", "Open project", "Ctrl+O"),
        ("Home", "主页", "Home", "Ctrl+1"),
        ("Settings", "设置", "Settings", "Ctrl+Comma"),
        ("Navigation", "显示／隐藏项目导航", "Toggle project navigation", "Ctrl+B"),
        ("Theme", "切换主题", "Toggle theme", "Ctrl+Shift+L")
    ];
    public static bool TryParse(string text, out VirtualKey key, out VirtualKeyModifiers modifiers)
    {
        key = VirtualKey.None; modifiers = VirtualKeyModifiers.None;
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;
        foreach (var part in parts[..^1])
        {
            var modifier = part.ToUpperInvariant() switch { "CTRL" => VirtualKeyModifiers.Control, "SHIFT" => VirtualKeyModifiers.Shift, "ALT" => VirtualKeyModifiers.Menu, _ => VirtualKeyModifiers.None };
            if (modifier == VirtualKeyModifiers.None || modifiers.HasFlag(modifier)) return false;
            modifiers |= modifier;
        }
        var name = parts[^1];
        if (name.Length == 1 && char.IsAsciiDigit(name[0])) name = "Number" + name;
        if (name == "Comma") { key = (VirtualKey)188; return true; }
        return Enum.TryParse(name, true, out key) && ((int)key is >= 48 and <= 90 or >= 112 and <= 135 || key is VirtualKey.Space or VirtualKey.Enter);
    }
    public static string Format(VirtualKey key, VirtualKeyModifiers modifiers) =>
        (modifiers.HasFlag(VirtualKeyModifiers.Control) ? "Ctrl+" : "") +
        (modifiers.HasFlag(VirtualKeyModifiers.Menu) ? "Alt+" : "") +
        (modifiers.HasFlag(VirtualKeyModifiers.Shift) ? "Shift+" : "") +
        ((int)key == 188 ? "Comma" : key.ToString().Replace("Number", ""));
}
