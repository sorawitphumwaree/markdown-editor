using FluentAssertions;
using System.IO;
using MarkdownEditor.Application.Abstractions;
using MarkdownEditor.Application.Documents;
using MarkdownEditor.App.ViewModels;
using MarkdownEditor.Domain.Documents;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MarkdownEditor.App.Tests;

public sealed class MainViewModelTests
{
    private readonly IDocumentFileStore _files = Substitute.For<IDocumentFileStore>();
    private readonly IRecoveryStore _recovery = Substitute.For<IRecoveryStore>();
    private readonly ILogger<MainViewModel> _logger = Substitute.For<ILogger<MainViewModel>>();

    [Fact]
    public async Task OpenAsync_DeduplicatesPathsIgnoringCaseAndSelectsExistingTab()
    {
        var path = Path.GetFullPath("notes.md");
        _files.OpenAsync(path, Arg.Any<CancellationToken>())
            .Returns(new Document(DocumentId.New(), path, "content"));
        var viewModel = CreateViewModel();

        var first = await viewModel.OpenAsync(path);
        var second = await viewModel.OpenAsync(path.ToUpperInvariant());

        second.Should().BeSameAs(first);
        viewModel.Documents.Should().ContainSingle();
        await _files.Received(1).OpenAsync(path, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Close_SelectsAdjacentTabAndClearsFinalSelection()
    {
        var viewModel = CreateViewModel();
        var first = viewModel.NewDocument();
        var second = viewModel.NewDocument();

        viewModel.Close(second);
        viewModel.ActiveDocument.Should().BeSameAs(first);
        viewModel.Close(first);

        viewModel.ActiveDocument.Should().BeNull();
        viewModel.Status.Should().Be("Ready");
    }

    [Fact]
    public async Task SaveAsync_DelegatesWorkflowAndUpdatesStatus()
    {
        var viewModel = CreateViewModel();
        var tab = viewModel.NewDocument();
        var destination = Path.GetFullPath("saved.md");
        _files.SaveAsync(tab.Model, destination, Arg.Any<CancellationToken>())
            .Returns(call => { tab.Model.MarkSaved(destination); return Task.CompletedTask; });

        await viewModel.SaveAsync(tab, destination);

        viewModel.Status.Should().Be("Saved saved.md");
        await _recovery.Received(1).RemoveSnapshotAsync(tab.Model.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ReorderDocument_PreservesTabAndMakesItActive()
    {
        var viewModel = CreateViewModel();
        var first = viewModel.NewDocument();
        var second = viewModel.NewDocument();

        viewModel.ReorderDocument(first, 2).Should().BeTrue();

        viewModel.Documents.Should().Equal(second, first);
        viewModel.ActiveDocument.Should().BeSameAs(first);
    }

    private MainViewModel CreateViewModel() =>
        new(new DocumentWorkflow(_files, _recovery), _logger);
}
