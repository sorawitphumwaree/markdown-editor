using System.Collections.ObjectModel;
using System.IO;

namespace MarkdownEditor.App.ViewModels;

public sealed class WorkspaceItemViewModel
{
    private bool _loaded;

    public WorkspaceItemViewModel(string path)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path);
        IsDirectory = Directory.Exists(path);
        if (IsDirectory)
            Children.Add(new WorkspaceItemViewModel());
    }

    private WorkspaceItemViewModel()
    {
        Path = string.Empty;
        Name = string.Empty;
    }

    public string Name { get; }
    public string Path { get; }
    public bool IsDirectory { get; }
    public ObservableCollection<WorkspaceItemViewModel> Children { get; } = [];

    public void LoadChildren()
    {
        if (_loaded || !IsDirectory)
            return;
        _loaded = true;
        Children.Clear();
        try
        {
            foreach (var directory in Directory.EnumerateDirectories(Path).Order())
                Children.Add(new WorkspaceItemViewModel(directory));
            foreach (var file in Directory.EnumerateFiles(Path)
                         .Where(IsDocumentationAsset).Order())
                Children.Add(new WorkspaceItemViewModel(file));
        }
        catch (UnauthorizedAccessException)
        {
            // Explorer keeps inaccessible folders collapsed.
        }
    }

    private static bool IsDocumentationAsset(string path)
    {
        var extension = System.IO.Path.GetExtension(path);
        return extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".svg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }
}
