namespace MarkdownEditor.Infrastructure.Tests;

public abstract class TemporaryDirectoryTest : IDisposable
{
    protected string DirectoryPath { get; } = Path.Combine(
        Path.GetTempPath(), $"MarkdownEditor.Tests.{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, true);
        GC.SuppressFinalize(this);
    }
}
