using FluentAssertions;
using MarkdownEditor.Domain.Documents;
using Xunit;

namespace MarkdownEditor.Domain.Tests;

public sealed class DocumentTests
{
    [Fact]
    public void NewDocument_DefaultsToPreviewMode()
    {
        var document = new Document(DocumentId.New(), "notes.md", string.Empty);

        document.ViewMode.Should().Be(DocumentViewMode.Preview);
    }

    [Fact]
    public void ReplaceContent_IncrementsVersionAndMarksDocumentModified()
    {
        var document = new Document(DocumentId.New(), "notes.md", "before");

        document.ReplaceContent("after");

        document.Version.Should().Be(1);
        document.IsModified.Should().BeTrue();
    }

    [Fact]
    public void MarkSaved_ResetsModifiedState()
    {
        var document = new Document(DocumentId.New(), "notes.md", "before");
        document.ReplaceContent("after");

        document.MarkSaved();

        document.IsModified.Should().BeFalse();
        document.LastSavedAt.Should().NotBeNull();
    }

    [Fact]
    public void ReloadFromDisk_ReplacesContentAndClearsModifiedState()
    {
        var document = new Document(DocumentId.New(), "reload.md", "original");
        document.ReplaceContent("edited");

        document.ReloadFromDisk("disk content");

        document.Content.Should().Be("disk content");
        document.IsModified.Should().BeFalse();
        document.Version.Should().Be(2);
    }
}
