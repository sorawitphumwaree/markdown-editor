using FluentAssertions;
using MarkdownEditor.Application.Documents;
using Xunit;

namespace MarkdownEditor.Application.Tests;

public sealed class TabReorderTests
{
    [Theory]
    [InlineData(4, 3, 2, 2)]
    [InlineData(4, 0, 3, 2)]
    [InlineData(4, 1, 4, 3)]
    [InlineData(4, 3, 0, 0)]
    public void GetDestinationIndex_TranslatesDropPosition(
        int tabCount,
        int sourceIndex,
        int insertionIndex,
        int expectedDestinationIndex)
    {
        var destinationIndex = TabReorder.GetDestinationIndex(
            tabCount,
            sourceIndex,
            insertionIndex);

        destinationIndex.Should().Be(expectedDestinationIndex);
    }

    [Theory]
    [InlineData(4, 1, 1)]
    [InlineData(4, 1, 2)]
    public void GetDestinationIndex_DetectsDropBesideOriginalPosition(
        int tabCount,
        int sourceIndex,
        int insertionIndex)
    {
        var destinationIndex = TabReorder.GetDestinationIndex(
            tabCount,
            sourceIndex,
            insertionIndex);

        destinationIndex.Should().Be(sourceIndex);
    }

    [Theory]
    [InlineData(0, 0, 0, "sourceIndex")]
    [InlineData(4, -1, 0, "sourceIndex")]
    [InlineData(4, 4, 0, "sourceIndex")]
    [InlineData(4, 0, -1, "insertionIndex")]
    [InlineData(4, 0, 5, "insertionIndex")]
    public void GetDestinationIndex_RejectsInvalidPositions(
        int tabCount,
        int sourceIndex,
        int insertionIndex,
        string parameterName)
    {
        var act = () => TabReorder.GetDestinationIndex(
            tabCount,
            sourceIndex,
            insertionIndex);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(parameterName);
    }
}
