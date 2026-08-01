namespace MarkdownEditor.Application.Paths;

public enum PathTargetKind
{
    Markdown,
    Image,
    ExternalUrl,
    OtherLocalFile,
    Missing,
    Unsafe,
    Unsupported
}

public sealed record PathResolutionResult(
    PathTargetKind Kind,
    string? FullPath,
    string? Fragment,
    bool IsInsideWorkspace,
    string? Error = null);

