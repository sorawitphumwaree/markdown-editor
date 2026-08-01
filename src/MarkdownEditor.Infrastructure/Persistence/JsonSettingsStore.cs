using System.Text.Json;
using MarkdownEditor.Application.Abstractions;
using MarkdownEditor.Application.Settings;

namespace MarkdownEditor.Infrastructure.Persistence;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    public JsonSettingsStore(string applicationDataDirectory)
    {
        _path = Path.Combine(applicationDataDirectory, "settings.json");
    }

    public async Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
            return new();
        try
        {
            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<ApplicationSettings>(
                stream, JsonOptions, cancellationToken) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }

    public async Task SaveAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }
}

