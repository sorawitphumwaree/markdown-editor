using System.Text;
using MarkdownEditor.Application.Abstractions;
using MarkdownEditor.Domain.Documents;

namespace MarkdownEditor.Infrastructure.FileSystem;

public sealed class LocalDocumentFileStore : IDocumentFileStore
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    public async Task<Document> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
        var readOnly = (File.GetAttributes(fullPath) & FileAttributes.ReadOnly) != 0;
        return new Document(DocumentId.New(), fullPath, content, readOnly);
    }

    public async Task SaveAsync(
        Document document,
        string? path = null,
        CancellationToken cancellationToken = default)
    {
        var destination = Path.GetFullPath(path ?? document.FilePath);
        var directory = Path.GetDirectoryName(destination)
            ?? throw new InvalidOperationException("The destination has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporary = Path.Combine(directory, $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporary, document.Content, Utf8WithoutBom, cancellationToken);
            File.Move(temporary, destination, true);
            document.MarkSaved(destination);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}

