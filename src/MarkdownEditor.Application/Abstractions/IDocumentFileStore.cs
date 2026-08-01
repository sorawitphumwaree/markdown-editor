using MarkdownEditor.Domain.Documents;

namespace MarkdownEditor.Application.Abstractions;

public interface IDocumentFileStore
{
    Task<Document> OpenAsync(string path, CancellationToken cancellationToken = default);
    Task SaveAsync(Document document, string? path = null, CancellationToken cancellationToken = default);
}

