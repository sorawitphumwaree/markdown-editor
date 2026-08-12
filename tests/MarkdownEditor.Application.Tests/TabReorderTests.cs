using System.Collections.ObjectModel;
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

    [Theory]
    [InlineData(0, 3, "B,C,A,D")]
    [InlineData(3, 0, "D,A,B,C")]
    [InlineData(1, 4, "A,C,D,B")]
    public void Move_ReordersExistingInstances(
        int sourceIndex,
        int insertionIndex,
        string expectedOrder)
    {
        var original = new[] { new Tab("A"), new Tab("B"), new Tab("C"), new Tab("D") };
        var items = new ObservableCollection<Tab>(original);
        var moved = original[sourceIndex];

        var changed = TabReorder.Move(items, sourceIndex, insertionIndex);

        changed.Should().BeTrue();
        items.Select(item => item.Name).Should().Equal(expectedOrder.Split(','));
        items.Should().ContainSingle(item => ReferenceEquals(item, moved));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Move_BesideOriginalPosition_IsNoOp(int insertionIndex)
    {
        var items = new ObservableCollection<string>(["A", "B", "C", "D"]);

        var changed = TabReorder.Move(items, 1, insertionIndex);

        changed.Should().BeFalse();
        items.Should().Equal("A", "B", "C", "D");
    }

    private sealed record Tab(string Name);
}
