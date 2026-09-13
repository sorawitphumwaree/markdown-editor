using MarkdownEditor.Application.Paths;

namespace MarkdownEditor.Infrastructure.Paths;

public sealed class WorkspacePathResolver : IWorkspacePathResolver
{
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".bmp" };

    public PathResolutionResult Resolve(
        string sourceDocumentPath,
        string referencedPath)
    {
        if (Uri.TryCreate(referencedPath, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme is "http" or "https" or "mailto"
                ? new(PathTargetKind.ExternalUrl, absoluteUri.ToString(), absoluteUri.Fragment)
                : new(PathTargetKind.Unsupported, null, null, "Unsupported URI scheme.");
        }

        var hashIndex = referencedPath.IndexOf('#', StringComparison.Ordinal);
        var rawPath = hashIndex >= 0 ? referencedPath[..hashIndex] : referencedPath;
        var fragment = hashIndex >= 0 ? Uri.UnescapeDataString(referencedPath[(hashIndex + 1)..]) : null;

        try
        {
            var decoded = Uri.UnescapeDataString(rawPath).Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(decoded))
            {
                var currentDocument = Path.GetFullPath(sourceDocumentPath);
                return new(PathTargetKind.Markdown, currentDocument, fragment);
            }

            var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourceDocumentPath))
                ?? throw new InvalidOperationException("Source document has no directory.");
            var target = Path.GetFullPath(Path.Combine(sourceDirectory, decoded));
            if (!File.Exists(target))
                return new(PathTargetKind.Missing, target, fragment, "Target does not exist.");

            var extension = Path.GetExtension(target);
            var kind = extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
                    ? PathTargetKind.Markdown
                    : ImageExtensions.Contains(extension)
                        ? PathTargetKind.Image
                        : PathTargetKind.OtherLocalFile;
            return new(kind, target, fragment);
        }
        catch (Exception ex) when (
            ex is ArgumentException or NotSupportedException or PathTooLongException or UriFormatException)
        {
            return new(PathTargetKind.Unsupported, null, fragment, ex.Message);
        }
    }
}
