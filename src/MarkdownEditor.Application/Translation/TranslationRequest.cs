using System.Text.RegularExpressions;

namespace MarkdownEditor.Application.Translation;

public static partial class TranslationRequest
{
    public static bool IsEligible(string? selection) =>
        !string.IsNullOrWhiteSpace(selection)
        && selection.Length <= 100
        && SingleWord().IsMatch(selection);

    [GeneratedRegex(@"^[\p{L}\p{M}]+(?:['’\-][\p{L}\p{M}]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SingleWord();
}
