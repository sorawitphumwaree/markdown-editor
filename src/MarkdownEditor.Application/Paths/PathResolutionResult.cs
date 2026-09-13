namespace MarkdownEditor.Application.Paths;

public enum PathTargetKind
{
    Markdown,
    Image,
    ExternalUrl,
    OtherLocalFile,
    Missing,
    Unsupported
}

public sealed record PathResolutionResult(
    PathTargetKind Kind,
    string? FullPath,
    string? Fragment,
    string? Error = null);
