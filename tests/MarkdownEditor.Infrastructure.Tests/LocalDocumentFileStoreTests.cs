using FluentAssertions;
using MarkdownEditor.Domain.Documents;
using MarkdownEditor.Infrastructure.FileSystem;
using Xunit;

namespace MarkdownEditor.Infrastructure.Tests;

public sealed class LocalDocumentFileStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"MarkdownEditor.Tests.{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveAndOpenAsync_RoundTripsUnicodeContent()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "unicode.md");
        var document = new Document(DocumentId.New(), path, "ภาษาไทย\nMermaid → Markdown");
        document.MarkDeleted();
        var store = new LocalDocumentFileStore();

        await store.SaveAsync(document);
        var reopened = await store.OpenAsync(path);

        reopened.Content.Should().Be(document.Content);
        document.ExistsOnDisk.Should().BeTrue();
        document.IsModified.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_ReplacesExistingFileAndLeavesNoTemporaryFile()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "existing.md");
        await File.WriteAllTextAsync(path, "old");
        var document = new Document(DocumentId.New(), path, "old");
        document.ReplaceContent("new");
        var store = new LocalDocumentFileStore();

        await store.SaveAsync(document);

        (await File.ReadAllTextAsync(path)).Should().Be("new");
        Directory.EnumerateFiles(_directory, "*.tmp").Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }
}
