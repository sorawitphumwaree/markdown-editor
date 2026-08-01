namespace MarkdownEditor.Domain.Documents;

public sealed class Document
{
    public Document(DocumentId id, string filePath, string content, bool isReadOnly = false)
    {
        Id = id;
        FilePath = Path.GetFullPath(filePath);
        Content = content;
        SavedContent = content;
        IsReadOnly = isReadOnly;
    }

    public DocumentId Id { get; }
    public string FilePath { get; private set; }
    public string DisplayName => Path.GetFileName(FilePath);
    public string Content { get; private set; }
    public string SavedContent { get; private set; }
    public int Version { get; private set; }
    public bool IsReadOnly { get; }
    public bool ExistsOnDisk { get; private set; } = true;
    public bool IsModified => !StringComparer.Ordinal.Equals(Content, SavedContent);
    public DateTimeOffset? LastSavedAt { get; private set; }
    public int CursorLine { get; set; } = 1;
    public int CursorColumn { get; set; } = 1;
    public double EditorScrollPosition { get; set; }
    public double PreviewScrollPosition { get; set; }
    public DocumentViewMode ViewMode { get; set; } = DocumentViewMode.Preview;

    public void ReplaceContent(string content)
    {
        Content = content;
        Version++;
    }

    public void MarkSaved(string? filePath = null)
    {
        if (filePath is not null)
            FilePath = Path.GetFullPath(filePath);

        SavedContent = Content;
        ExistsOnDisk = true;
        LastSavedAt = DateTimeOffset.UtcNow;
    }

    public void ReloadFromDisk(string content)
    {
        Content = content;
        SavedContent = content;
        ExistsOnDisk = true;
        Version++;
    }

    public void MarkDeleted() => ExistsOnDisk = false;
}
