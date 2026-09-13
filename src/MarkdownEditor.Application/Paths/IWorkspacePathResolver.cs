namespace MarkdownEditor.Application.Paths;

public interface IWorkspacePathResolver
{
    PathResolutionResult Resolve(string sourceDocumentPath, string referencedPath);
}

