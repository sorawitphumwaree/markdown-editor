using MarkdownEditor.Application.Settings;

namespace MarkdownEditor.Application.Abstractions;

public interface ISettingsStore
{
    Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);
}

