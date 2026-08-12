using FluentAssertions;
using MarkdownEditor.Domain.Documents;
using MarkdownEditor.Infrastructure.Recovery;
using Xunit;

namespace MarkdownEditor.Infrastructure.Tests;

public sealed class JsonRecoveryStoreTests : TemporaryDirectoryTest
{
    [Fact]
    public async Task SaveAndLoadSnapshotsAsync_RoundTripsAndSortsNewestFirst()
    {
        var store = new JsonRecoveryStore(DirectoryPath);
        var first = new Document(DocumentId.New(), Path.Combine(DirectoryPath, "first.md"), "first");
        var second = new Document(DocumentId.New(), Path.Combine(DirectoryPath, "second.md"), "second");

        await store.SaveSnapshotAsync(first);
        await Task.Delay(20);
        await store.SaveSnapshotAsync(second);

        var snapshots = await store.LoadSnapshotsAsync();
        snapshots.Select(item => item.DocumentId).Should().Equal(second.Id, first.Id);
        snapshots[0].Content.Should().Be("second");
    }

    [Fact]
    public async Task LoadSnapshotsAsync_IgnoresCorruptFiles()
    {
        var recovery = Path.Combine(DirectoryPath, "Recovery");
        Directory.CreateDirectory(recovery);
        await File.WriteAllTextAsync(Path.Combine(recovery, "broken.json"), "not json");

        var snapshots = await new JsonRecoveryStore(DirectoryPath).LoadSnapshotsAsync();

        snapshots.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveSnapshotAsync_RemovesOnlyRequestedDocument()
    {
        var store = new JsonRecoveryStore(DirectoryPath);
        var keep = new Document(DocumentId.New(), Path.Combine(DirectoryPath, "keep.md"), "keep");
        var remove = new Document(DocumentId.New(), Path.Combine(DirectoryPath, "remove.md"), "remove");
        await store.SaveSnapshotAsync(keep);
        await store.SaveSnapshotAsync(remove);

        await store.RemoveSnapshotAsync(remove.Id);

        (await store.LoadSnapshotsAsync()).Select(item => item.DocumentId).Should().Equal(keep.Id);
    }
}
