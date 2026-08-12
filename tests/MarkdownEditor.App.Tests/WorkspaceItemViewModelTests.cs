using FluentAssertions;
using System.IO;
using MarkdownEditor.App.ViewModels;
using Xunit;

namespace MarkdownEditor.App.Tests;

public sealed class WorkspaceItemViewModelTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"MarkdownEditor.App.Tests.{Guid.NewGuid():N}");

    [Fact]
    public void LoadChildren_IncludesDocumentationAssetsAndDirectoriesOnly()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "nested"));
        File.WriteAllText(Path.Combine(_directory, "readme.md"), "# Readme");
        File.WriteAllText(Path.Combine(_directory, "diagram.svg"), "<svg/>");
        File.WriteAllText(Path.Combine(_directory, "ignored.txt"), "ignored");
        var item = new WorkspaceItemViewModel(_directory);

        item.LoadChildren();

        item.Children.Select(child => child.Name)
            .Should().Equal("nested", "diagram.svg", "readme.md");
        item.LoadChildren();
        item.Children.Should().HaveCount(3);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
        GC.SuppressFinalize(this);
    }
}
