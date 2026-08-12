using FluentAssertions;
using MarkdownEditor.App.ViewModels;
using MarkdownEditor.Domain.Documents;
using Xunit;

namespace MarkdownEditor.App.Tests;

public sealed class DocumentTabViewModelTests
{
    [Fact]
    public void ReplaceContent_MarksHeaderAndRaisesNotification()
    {
        var model = new Document(DocumentId.New(), "notes.md", "before");
        var tab = new DocumentTabViewModel(model);
        var changed = new List<string?>();
        tab.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        tab.ReplaceContent("after");

        tab.Header.Should().EndWith(" •");
        model.Content.Should().Be("after");
        changed.Should().Contain(nameof(tab.Header));
    }

    [Fact]
    public void MarkSaved_RaisesAllPathDependentProperties()
    {
        var model = new Document(DocumentId.New(), "draft.md", "content");
        var tab = new DocumentTabViewModel(model);
        var changed = new List<string?>();
        tab.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        model.MarkSaved("published.md");

        tab.MarkSaved();

        changed.Should().Contain([nameof(tab.FilePath), nameof(tab.DisplayName), nameof(tab.Header)]);
        tab.DisplayName.Should().Be("published.md");
    }
}
