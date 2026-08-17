using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MarkdownEditor.Application.Translation;

namespace MarkdownEditor.Infrastructure.Translation;

public sealed class MyMemoryTranslationProvider(HttpClient httpClient) : ITranslationProvider
{
    public async Task<TranslationResult> TranslateWordAsync(
        string word,
        string sourceLanguage,
        string destinationLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!sourceLanguage.Equals("en", StringComparison.OrdinalIgnoreCase)
            || !destinationLanguage.Equals("th", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("This release supports English to Thai translation only.");
        if (!TranslationRequest.IsEligible(word))
            throw new ArgumentException("Select exactly one word to translate.", nameof(word));

        var encodedWord = Uri.EscapeDataString(word);
        var translationTask = httpClient.GetFromJsonAsync<MyMemoryResponse>(
            $"https://api.mymemory.translated.net/get?q={encodedWord}&langpair=en%7Cth",
            cancellationToken);
        var dictionaryTask = httpClient.GetFromJsonAsync<List<DictionaryEntry>>(
            $"https://api.dictionaryapi.dev/api/v2/entries/en/{encodedWord}",
            cancellationToken);

        MyMemoryResponse? response;
        List<DictionaryEntry>? dictionary = null;
        try
        {
            response = await translationTask;
            try { dictionary = await dictionaryTask; }
            catch (HttpRequestException) { }
        }
        catch
        {
            try { await dictionaryTask; } catch { }
            throw;
        }

        var translations = new[] { response?.ResponseData?.TranslatedText }
            .Concat(response?.Matches?.Select(match => match.Translation) ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToArray();
        if (translations.Length == 0)
            throw new InvalidOperationException("No Thai translation was available for this word.");

        var partOfSpeech = dictionary?
            .SelectMany(entry => entry.Meanings ?? [])
            .Select(meaning => meaning.PartOfSpeech)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        return new(word, "en", "th", partOfSpeech, translations);
    }

    private sealed record MyMemoryResponse(
        [property: JsonPropertyName("responseData")] ResponseData? ResponseData,
        [property: JsonPropertyName("matches")] List<Match>? Matches);
    private sealed record ResponseData([property: JsonPropertyName("translatedText")] string? TranslatedText);
    private sealed record Match([property: JsonPropertyName("translation")] string? Translation);
    private sealed record DictionaryEntry([property: JsonPropertyName("meanings")] List<Meaning>? Meanings);
    private sealed record Meaning([property: JsonPropertyName("partOfSpeech")] string? PartOfSpeech);
}
