namespace MarkdownEditor.Domain.Workspaces;

public sealed record Workspace
{
    public Workspace(string rootPath) => RootPath = Path.GetFullPath(rootPath);
    public string RootPath { get; }
}

