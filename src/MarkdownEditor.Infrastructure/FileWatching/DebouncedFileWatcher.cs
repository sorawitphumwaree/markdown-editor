namespace MarkdownEditor.Infrastructure.FileWatching;

public sealed class DebouncedFileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly TimeSpan _debounce;
    private readonly Dictionary<string, DateTimeOffset> _lastEvents = new(StringComparer.OrdinalIgnoreCase);

    public DebouncedFileWatcher(string rootPath, TimeSpan? debounce = null)
    {
        _debounce = debounce ?? TimeSpan.FromMilliseconds(350);
        _watcher = new(rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += Handle;
        _watcher.Deleted += Handle;
        _watcher.Renamed += Handle;
        _watcher.EnableRaisingEvents = true;
    }

    public event EventHandler<string>? FileChanged;

    private void Handle(object sender, FileSystemEventArgs args)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_lastEvents)
        {
            if (_lastEvents.TryGetValue(args.FullPath, out var last) && now - last < _debounce)
                return;
            _lastEvents[args.FullPath] = now;
        }
        FileChanged?.Invoke(this, args.FullPath);
    }

    public void Dispose() => _watcher.Dispose();
}

