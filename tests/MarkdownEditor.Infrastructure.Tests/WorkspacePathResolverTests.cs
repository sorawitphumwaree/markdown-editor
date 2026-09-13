using FluentAssertions;
using MarkdownEditor.Application.Paths;
using MarkdownEditor.Infrastructure.Paths;
using Xunit;

namespace MarkdownEditor.Infrastructure.Tests;

public sealed class WorkspacePathResolverTests
{
    [Fact]
    public void Resolve_ExternalHttpsUrl_ClassifiesAsExternal()
    {
        var result = new WorkspacePathResolver().Resolve(
            @"C:\workspace\docs\readme.md",
            "https://example.com/page");

        result.Kind.Should().Be(PathTargetKind.ExternalUrl);
    }

    [Fact]
    public void Resolve_RelativePathEscapingDocumentDirectory_IsMissingNotUnsafe()
    {
        var result = new WorkspacePathResolver().Resolve(
            @"C:\workspace\docs\readme.md",
            "../../secret.md");

        result.Kind.Should().Be(PathTargetKind.Missing);
        result.FullPath.Should().Be(@"C:\secret.md");
    }

    [Fact]
    public void Resolve_SameDocumentFragment_ReturnsCurrentMarkdownDocument()
    {
        var source = @"C:\workspace\docs\readme.md";

        var result = new WorkspacePathResolver().Resolve(
            source,
            "#system-design");

        result.Kind.Should().Be(PathTargetKind.Markdown);
        result.FullPath.Should().Be(source);
        result.Fragment.Should().Be("system-design");
    }

    [Fact]
    public void Resolve_UnsupportedAbsoluteScheme_IsRejected()
    {
        var result = new WorkspacePathResolver().Resolve(
            @"C:\workspace\docs\readme.md",
            "file:///C:/Windows/System32/config");

        result.Kind.Should().Be(PathTargetKind.Unsupported);
    }
}
