namespace MarkdownEditor.Application.Translation;

public interface ITranslationProvider
{
    Task<TranslationResult> TranslateWordAsync(
        string word,
        string sourceLanguage,
        string destinationLanguage,
        CancellationToken cancellationToken = default);
}
