namespace MarkdownEditor.Application.Documents;

public static class TabReorder
{
    public static int GetDestinationIndex(
        int tabCount,
        int sourceIndex,
        int insertionIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tabCount);

        if (sourceIndex < 0 || sourceIndex >= tabCount)
            throw new ArgumentOutOfRangeException(nameof(sourceIndex));
        if (insertionIndex < 0 || insertionIndex > tabCount)
            throw new ArgumentOutOfRangeException(nameof(insertionIndex));

        return sourceIndex < insertionIndex
            ? insertionIndex - 1
            : insertionIndex;
    }
}
