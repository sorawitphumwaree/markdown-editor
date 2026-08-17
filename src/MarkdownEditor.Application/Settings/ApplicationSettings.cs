namespace MarkdownEditor.Application.Settings;

public sealed class ApplicationSettings
{
    public int SchemaVersion { get; init; } = 1;
    public string Theme { get; set; } = "system";
    public bool RestorePreviousSession { get; set; } = true;
    public bool ConfirmExternalLinks { get; set; } = true;
    public bool OpenPdfAfterExport { get; set; } = true;
    public string? LastWorkspace { get; set; }
    public double SidebarWidth { get; set; } = 260;
    public double SplitRatio { get; set; } = 0.5;
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 800;
    public string TranslationSourceLanguage { get; set; } = "en";
    public string TranslationDestinationLanguage { get; set; } = "th";
}
