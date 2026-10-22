namespace Tomography.Business.Models;

public sealed class AppearanceSettings
{
    public string Mode { get; set; } = "Default";
    public bool SeparateModes { get; set; }
    public string ReduceMotion { get; set; } = "Default";
    public bool PointerCursor { get; set; }
    public AppearancePalette Shared { get; set; } = new();
    public AppearancePalette Light { get; set; } = new() { Background = "#F4F5F7", Foreground = "#202020" };
    public AppearancePalette Dark { get; set; } = new();
}

public sealed class AppearancePalette
{
    public string Name { get; set; } = "FieldSync";
    public string Accent { get; set; } = "#18B568";
    public string Background { get; set; } = "#181818";
    public string Foreground { get; set; } = "#FFFFFF";
    public string Font { get; set; } = "Segoe UI Variable Text";
    public string ContentFont { get; set; } = "";
    public string CodeFont { get; set; } = "Cascadia Mono";
    public string FontStyle { get; set; } = "常规";
    public string ContentStyle { get; set; } = "常规";
    public string CodeStyle { get; set; } = "常规";
    public double FontSize { get; set; } = 14;
    public double CodeSize { get; set; } = 12;
    public bool TranslucentSidebar { get; set; }
    public double Contrast { get; set; } = 60;
}
