using FluentAssertions;
using MarkdownEditor.Application.Translation;
using Xunit;

namespace MarkdownEditor.Application.Tests;

public sealed class TranslationRequestTests
{
    [Theory]
    [InlineData("factory")]
    [InlineData("can't")]
    [InlineData("well-being")]
    public void IsEligible_WithOneWord_ReturnsTrue(string word) =>
        TranslationRequest.IsEligible(word).Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("two words")]
    [InlineData("word.")]
    public void IsEligible_WithoutExactlyOneWord_ReturnsFalse(string selection) =>
        TranslationRequest.IsEligible(selection).Should().BeFalse();
}
