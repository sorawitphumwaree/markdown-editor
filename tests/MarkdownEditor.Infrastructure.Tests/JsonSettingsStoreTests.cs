using FluentAssertions;
using MarkdownEditor.Application.Settings;
using MarkdownEditor.Infrastructure.Persistence;
using Xunit;

namespace MarkdownEditor.Infrastructure.Tests;

public sealed class JsonSettingsStoreTests : TemporaryDirectoryTest
{
    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsDefaults()
    {
        var settings = await new JsonSettingsStore(DirectoryPath).LoadAsync();

        settings.SchemaVersion.Should().Be(1);
        settings.RestorePreviousSession.Should().BeTrue();
        settings.SplitRatio.Should().Be(0.5);
        settings.ShowTableOfContents.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsSettings()
    {
        var store = new JsonSettingsStore(DirectoryPath);
        var expected = new ApplicationSettings
        {
            Theme = "light",
            ShowTableOfContents = false,
            RestorePreviousSession = false,
            SplitRatio = 0.7,
            TranslationSourceLanguage = "th",
            TranslationDestinationLanguage = "en"
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task LoadAsync_WhenOlderSettingsOmitContentsPreference_DefaultsToVisible()
    {
        Directory.CreateDirectory(DirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(DirectoryPath, "settings.json"),
            "{\"Theme\":\"light\"}");

        var settings = await new JsonSettingsStore(DirectoryPath).LoadAsync();

        settings.ShowTableOfContents.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveAsync_RestoresLatestContentsPreferenceInANewStore(bool visible)
    {
        var store = new JsonSettingsStore(DirectoryPath);
        var settings = await store.LoadAsync();
        settings.ShowTableOfContents = !visible;
        await store.SaveAsync(settings);
        settings.ShowTableOfContents = visible;
        await store.SaveAsync(settings);

        var restored = await new JsonSettingsStore(DirectoryPath).LoadAsync();

        restored.ShowTableOfContents.Should().Be(visible);
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsCorrupt_ReturnsDefaults()
    {
        Directory.CreateDirectory(DirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(DirectoryPath, "settings.json"), "{broken");

        var settings = await new JsonSettingsStore(DirectoryPath).LoadAsync();

        settings.Should().BeEquivalentTo(new ApplicationSettings());
    }
}
