using FluentAssertions;
using MarkdownEditor.Domain.Documents;
using MarkdownEditor.Infrastructure.FileSystem;
using Xunit;

namespace MarkdownEditor.Infrastructure.Tests;

public sealed class LocalDocumentFileStoreTests : TemporaryDirectoryTest
{
    [Fact]
    public async Task SaveAndOpenAsync_RoundTripsUnicodeContent()
    {
        Directory.CreateDirectory(DirectoryPath);
        var path = Path.Combine(DirectoryPath, "unicode.md");
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
        Directory.CreateDirectory(DirectoryPath);
        var path = Path.Combine(DirectoryPath, "existing.md");
        await File.WriteAllTextAsync(path, "old");
        var document = new Document(DocumentId.New(), path, "old");
        document.ReplaceContent("new");
        var store = new LocalDocumentFileStore();

        await store.SaveAsync(document);

        (await File.ReadAllTextAsync(path)).Should().Be("new");
        Directory.EnumerateFiles(DirectoryPath, "*.tmp").Should().BeEmpty();
    }

    [Fact]
    public async Task OpenAsync_DetectsReadOnlyFiles()
    {
        Directory.CreateDirectory(DirectoryPath);
        var path = Path.Combine(DirectoryPath, "read-only.md");
        await File.WriteAllTextAsync(path, "content");
        File.SetAttributes(path, FileAttributes.ReadOnly);

        try
        {
            var document = await new LocalDocumentFileStore().OpenAsync(path);
            document.IsReadOnly.Should().BeTrue();
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    [Fact]
    public async Task SaveAsync_ToNewNestedPath_UpdatesDocumentIdentity()
    {
        var original = Path.Combine(DirectoryPath, "draft.md");
        var destination = Path.Combine(DirectoryPath, "nested", "saved.md");
        var document = new Document(DocumentId.New(), original, "content");
        document.MarkDeleted();

        await new LocalDocumentFileStore().SaveAsync(document, destination);

        File.Exists(destination).Should().BeTrue();
        document.FilePath.Should().Be(Path.GetFullPath(destination));
        document.ExistsOnDisk.Should().BeTrue();
        document.IsModified.Should().BeFalse();
    }
}
