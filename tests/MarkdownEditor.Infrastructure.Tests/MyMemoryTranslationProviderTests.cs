using System.Net;
using System.Text;
using FluentAssertions;
using MarkdownEditor.Infrastructure.Translation;
using Xunit;

namespace MarkdownEditor.Infrastructure.Tests;

public sealed class MyMemoryTranslationProviderTests
{
    [Fact]
    public async Task TranslateWordAsync_MapsTranslationAlternativesAndPartOfSpeech()
    {
        using var client = new HttpClient(new StubHandler(request =>
            request.RequestUri!.Host.Contains("dictionaryapi", StringComparison.Ordinal)
                ? """[{"meanings":[{"partOfSpeech":"noun"}]}]"""
                : """{"responseData":{"translatedText":"โรงงาน"},"matches":[{"translation":"กิจการ"},{"translation":"โรงงาน"}]}"""));
        var provider = new MyMemoryTranslationProvider(client);

        var result = await provider.TranslateWordAsync("factory", "en", "th");

        result.SourceWord.Should().Be("factory");
        result.PartOfSpeech.Should().Be("noun");
        result.Translations.Should().Equal("โรงงาน", "กิจการ");
    }

    [Fact]
    public async Task TranslateWordAsync_WithUnsupportedPair_FailsClearly()
    {
        using var client = new HttpClient(new StubHandler(_ => "{}"));
        var provider = new MyMemoryTranslationProvider(client);

        var action = () => provider.TranslateWordAsync("factory", "th", "en");

        await action.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*English to Thai*");
    }

    private sealed class StubHandler(Func<HttpRequestMessage, string> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response(request), Encoding.UTF8, "application/json")
            });
    }
}
