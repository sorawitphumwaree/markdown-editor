namespace MarkdownEditor.Application.Translation;

public sealed record TranslationResult(
    string SourceWord,
    string SourceLanguage,
    string DestinationLanguage,
    string? PartOfSpeech,
    IReadOnlyList<string> Translations);
