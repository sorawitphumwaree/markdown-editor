using FluentAssertions;
using MarkdownEditor.Application.Abstractions;
using MarkdownEditor.Application.Documents;
using MarkdownEditor.Domain.Documents;
using NSubstitute;
using Xunit;

namespace MarkdownEditor.Application.Tests;

public sealed class DocumentWorkflowTests
{
    private readonly IDocumentFileStore _fileStore = Substitute.For<IDocumentFileStore>();
    private readonly IRecoveryStore _recoveryStore = Substitute.For<IRecoveryStore>();

    [Fact]
    public async Task UpdateContentAsync_UpdatesDocumentAndWritesRecoverySnapshot()
    {
        var document = new Document(DocumentId.New(), "notes.md", "before");
        var workflow = new DocumentWorkflow(_fileStore, _recoveryStore);

        await workflow.UpdateContentAsync(document, "after");

        document.Content.Should().Be("after");
        document.Version.Should().Be(1);
        await _recoveryStore.Received(1).SaveSnapshotAsync(document, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_PersistsDocumentBeforeRemovingRecoverySnapshot()
    {
        var document = new Document(DocumentId.New(), "notes.md", "content");
        var calls = new List<string>();
        _fileStore.SaveAsync(document, null, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("save");
                return Task.CompletedTask;
            });
        _recoveryStore.RemoveSnapshotAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("remove-recovery");
                return Task.CompletedTask;
            });
        var workflow = new DocumentWorkflow(_fileStore, _recoveryStore);

        await workflow.SaveAsync(document);

        calls.Should().Equal("save", "remove-recovery");
    }

    [Fact]
    public async Task UpdateContentAsync_WhenUndoReturnsToSavedContent_RemovesRecoverySnapshot()
    {
        var document = new Document(DocumentId.New(), "notes.md", "saved");
        var workflow = new DocumentWorkflow(_fileStore, _recoveryStore);
        await workflow.UpdateContentAsync(document, "edited");

        await workflow.UpdateContentAsync(document, "saved");

        document.IsModified.Should().BeFalse();
        await _recoveryStore.Received(1)
            .RemoveSnapshotAsync(document.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReloadAsync_PreservesViewportState()
    {
        var document = new Document(DocumentId.New(), "notes.md", "before")
        {
            CursorLine = 8,
            EditorScrollPosition = 120,
            PreviewScrollPosition = 340
        };
        _fileStore.OpenAsync(document.FilePath, Arg.Any<CancellationToken>())
            .Returns(new Document(DocumentId.New(), document.FilePath, "from disk"));
        var workflow = new DocumentWorkflow(_fileStore, _recoveryStore);

        await workflow.ReloadAsync(document);

        document.Content.Should().Be("from disk");
        document.CursorLine.Should().Be(8);
        document.EditorScrollPosition.Should().Be(120);
        document.PreviewScrollPosition.Should().Be(340);
    }
}
