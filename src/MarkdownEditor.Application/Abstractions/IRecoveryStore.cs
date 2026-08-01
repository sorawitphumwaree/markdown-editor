using MarkdownEditor.Domain.Documents;

namespace MarkdownEditor.Application.Abstractions;

public interface IRecoveryStore
{
    Task SaveSnapshotAsync(Document document, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecoverySnapshot>> LoadSnapshotsAsync(CancellationToken cancellationToken = default);
    Task RemoveSnapshotAsync(DocumentId id, CancellationToken cancellationToken = default);
}

public sealed record RecoverySnapshot(
    DocumentId DocumentId,
    string FilePath,
    string Content,
    DateTimeOffset CapturedAt);

