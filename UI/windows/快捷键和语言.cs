using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Tomography.UI.Controls;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private readonly List<KeyboardAccelerator> _shortcuts = new();
    private void InitializeInterfacePreferences()
    {
        SideRail.LanguageRequested += (_, language) =>
        {
            var previous = _globalSettings.Language;
            try { _globalSettings.Language = language; SaveGlobalSettings(); ApplyInterfaceLanguage(); }
            catch { _globalSettings.Language = previous; HomePanel.SetStatus("语言设置保存失败。"); }
        };
        AiSettingsPanel.Shortcuts.LoadBindings(_globalSettings.Shortcuts);
        AiSettingsPanel.Shortcuts.SaveRequested += (_, shortcuts) =>
        {
            var previous = _globalSettings.Shortcuts;
            try { _globalSettings.Shortcuts = shortcuts; SaveGlobalSettings(); RebuildShortcuts(); }
            catch { _globalSettings.Shortcuts = previous; throw; }
        };
        RebuildShortcuts(); ApplyInterfaceLanguage();
    }
    private void ApplyInterfaceLanguage()
    {
        var english = _globalSettings.Language == "en";
        InterfaceLanguage.Apply(Root, english);
        SideRail.SetLanguage(_globalSettings.Language);
        AiSettingsPanel.SetLanguage(english);
    }
    private void RebuildShortcuts()
    {
        foreach (var shortcut in _shortcuts) Root.KeyboardAccelerators.Remove(shortcut);
        _shortcuts.Clear();
        foreach (var pair in _globalSettings.Shortcuts)
        {
            if (!ApplicationShortcuts.Actions.Any(a => a.Id == pair.Key) || !ApplicationShortcuts.TryParse(pair.Value, out var key, out var modifiers)) continue;
            var shortcut = new KeyboardAccelerator { Key = key, Modifiers = modifiers, ScopeOwner = Root };
            shortcut.Invoked += async (_, args) =>
            {
                // Shortcut recording is an editing operation, not an application command.
                var focus = FocusManager.GetFocusedElement(Root.XamlRoot) as FrameworkElement;
                if (focus is not null && Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(focus).StartsWith("Shortcut_")) return;
                args.Handled = true;
                switch (pair.Key)
                {
                    case "NewProject": await CreateProjectAsync(); break;
                    case "OpenProject": await BrowseProjectAsync(); break;
                    case "NewChat": if (!_aiBusy) NewAiConversation(); break;
                    case "Home": ViewModel.Section = "主页"; SideRail.SetSection("主页"); break;
                    case "Settings": ViewModel.Section = "设置"; SideRail.SetSection("设置"); break;
                    case "Navigation": ToggleProjectNavigation(this, EventArgs.Empty); break;
                    case "Theme": SideRail.SwitchTheme(); break;
                }
            };
            Root.KeyboardAccelerators.Add(shortcut); _shortcuts.Add(shortcut);
        }
    }
}
