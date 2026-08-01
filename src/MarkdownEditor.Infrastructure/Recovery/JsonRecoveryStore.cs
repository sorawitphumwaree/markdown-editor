using System.Text.Json;
using MarkdownEditor.Application.Abstractions;
using MarkdownEditor.Domain.Documents;

namespace MarkdownEditor.Infrastructure.Recovery;

public sealed class JsonRecoveryStore : IRecoveryStore
{
    private readonly string _directory;

    public JsonRecoveryStore(string applicationDataDirectory)
    {
        _directory = Path.Combine(applicationDataDirectory, "Recovery");
    }

    public async Task SaveSnapshotAsync(Document document, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_directory);
        var snapshot = new RecoverySnapshot(
            document.Id, document.FilePath, document.Content, DateTimeOffset.UtcNow);
        await using var stream = File.Create(GetPath(document.Id));
        await JsonSerializer.SerializeAsync(stream, snapshot, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<RecoverySnapshot>> LoadSnapshotsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_directory))
            return [];

        var snapshots = new List<RecoverySnapshot>();
        foreach (var path in Directory.EnumerateFiles(_directory, "*.json"))
        {
            try
            {
                await using var stream = File.OpenRead(path);
                var snapshot = await JsonSerializer.DeserializeAsync<RecoverySnapshot>(
                    stream, cancellationToken: cancellationToken);
                if (snapshot is not null)
                    snapshots.Add(snapshot);
            }
            catch (JsonException)
            {
                // A corrupt snapshot must not prevent startup.
            }
        }
        return snapshots.OrderByDescending(item => item.CapturedAt).ToArray();
    }

    public Task RemoveSnapshotAsync(
        DocumentId id,
        CancellationToken cancellationToken = default)
    {
        File.Delete(GetPath(id));
        return Task.CompletedTask;
    }

    private string GetPath(DocumentId id) => Path.Combine(_directory, $"{id}.json");
}

