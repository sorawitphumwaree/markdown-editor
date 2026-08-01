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
            "https://example.com/page",
            @"C:\workspace");

        result.Kind.Should().Be(PathTargetKind.ExternalUrl);
    }

    [Fact]
    public void Resolve_PathOutsideWorkspace_IsUnsafe()
    {
        var result = new WorkspacePathResolver().Resolve(
            @"C:\workspace\docs\readme.md",
            "../../secret.md",
            @"C:\workspace");

        result.Kind.Should().Be(PathTargetKind.Unsafe);
    }

    [Fact]
    public void Resolve_SameDocumentFragment_ReturnsCurrentMarkdownDocument()
    {
        var source = @"C:\workspace\docs\readme.md";

        var result = new WorkspacePathResolver().Resolve(
            source,
            "#system-design",
            @"C:\workspace");

        result.Kind.Should().Be(PathTargetKind.Markdown);
        result.FullPath.Should().Be(source);
        result.Fragment.Should().Be("system-design");
    }

    [Fact]
    public void Resolve_UnsupportedAbsoluteScheme_IsRejected()
    {
        var result = new WorkspacePathResolver().Resolve(
            @"C:\workspace\docs\readme.md",
            "file:///C:/Windows/System32/config",
            @"C:\workspace");

        result.Kind.Should().Be(PathTargetKind.Unsupported);
    }
}
