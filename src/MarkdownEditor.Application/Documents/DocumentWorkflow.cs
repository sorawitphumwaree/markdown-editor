using System.Collections.Concurrent;
using MarkdownEditor.Application.Abstractions;
using MarkdownEditor.Domain.Documents;

namespace MarkdownEditor.Application.Documents;

/// <summary>
/// Coordinates document persistence and recovery independently of the desktop UI.
/// </summary>
public sealed class DocumentWorkflow(
    IDocumentFileStore fileStore,
    IRecoveryStore recoveryStore)
{
    private readonly ConcurrentDictionary<DocumentId, SemaphoreSlim> _recoveryLocks = new();

    public Task<Document> OpenAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        fileStore.OpenAsync(path, cancellationToken);

    public async Task SaveAsync(
        Document document,
        string? path = null,
        CancellationToken cancellationToken = default)
    {
        await fileStore.SaveAsync(document, path, cancellationToken);
        await recoveryStore.RemoveSnapshotAsync(document.Id, cancellationToken);
    }

    public async Task ReloadAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        var diskDocument = await fileStore.OpenAsync(document.FilePath, cancellationToken);
        document.ReloadFromDisk(diskDocument.Content);
        await recoveryStore.RemoveSnapshotAsync(document.Id, cancellationToken);
    }

    public async Task UpdateContentAsync(
        Document document,
        string content,
        CancellationToken cancellationToken = default)
    {
        document.ReplaceContent(content);
        var recoveryLock = _recoveryLocks.GetOrAdd(document.Id, _ => new SemaphoreSlim(1, 1));
        await recoveryLock.WaitAsync(cancellationToken);
        try
        {
            if (document.IsModified)
                await recoveryStore.SaveSnapshotAsync(document, cancellationToken);
            else
                await recoveryStore.RemoveSnapshotAsync(document.Id, cancellationToken);
        }
        finally
        {
            recoveryLock.Release();
        }
    }
}
