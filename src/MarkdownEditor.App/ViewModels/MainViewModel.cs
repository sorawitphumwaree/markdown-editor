using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using MarkdownEditor.Application.Documents;
using MarkdownEditor.Domain.Documents;
using Microsoft.Extensions.Logging;

namespace MarkdownEditor.App.ViewModels;

public sealed partial class MainViewModel(
    DocumentWorkflow documentWorkflow,
    ILogger<MainViewModel> logger) : ObservableObject
{
    [ObservableProperty]
    private DocumentTabViewModel? _activeDocument;

    [ObservableProperty]
    private string? _workspacePath;

    [ObservableProperty]
    private string _status = "Ready";

    public ObservableCollection<DocumentTabViewModel> Documents { get; } = [];
    public ObservableCollection<WorkspaceItemViewModel> WorkspaceItems { get; } = [];

    public DocumentTabViewModel NewDocument()
    {
        var documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var number = 1;
        string path;
        do
        {
            var suffix = number == 1 ? string.Empty : $"-{number}";
            path = Path.Combine(documentsDirectory, $"Untitled{suffix}.md");
            number++;
        }
        while (File.Exists(path)
            || Documents.Any(item => StringComparer.OrdinalIgnoreCase.Equals(item.FilePath, path)));

        var document = new Document(DocumentId.New(), path, string.Empty);
        document.MarkDeleted();
        document.ViewMode = DocumentViewMode.Split;
        var tab = new DocumentTabViewModel(document);
        Documents.Add(tab);
        ActiveDocument = tab;
        Status = "New document";
        return tab;
    }

    public void Close(DocumentTabViewModel document)
    {
        var index = Documents.IndexOf(document);
        Documents.Remove(document);
        ActiveDocument = Documents.Count == 0
            ? null
            : Documents[Math.Min(index, Documents.Count - 1)];
        Status = Documents.Count == 0 ? "Ready" : $"Active: {ActiveDocument!.DisplayName}";
    }

    public async Task<DocumentTabViewModel> OpenAsync(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var existing = Documents.FirstOrDefault(
            item => StringComparer.OrdinalIgnoreCase.Equals(item.FilePath, fullPath));
        if (existing is not null)
        {
            ActiveDocument = existing;
            return existing;
        }

        var document = await documentWorkflow.OpenAsync(fullPath);
        var tab = new DocumentTabViewModel(document);
        Documents.Add(tab);
        ActiveDocument = tab;
        Status = $"Opened {tab.DisplayName}";
        logger.LogInformation("Opened document {Path}", fullPath);
        return tab;
    }

    public async Task SaveAsync(DocumentTabViewModel document, string? path = null)
    {
        await documentWorkflow.SaveAsync(document.Model, path);
        document.MarkSaved();
        Status = $"Saved {document.DisplayName}";
        logger.LogInformation("Saved document {Path}", document.FilePath);
    }

    public async Task ReloadAsync(DocumentTabViewModel document)
    {
        await documentWorkflow.ReloadAsync(document.Model);
        document.MarkReloaded();
        Status = $"Reloaded {document.DisplayName}";
        logger.LogInformation("Reloaded document {Path}", document.FilePath);
    }

    public async Task UpdateContentAsync(DocumentTabViewModel document, string content)
    {
        await documentWorkflow.UpdateContentAsync(document.Model, content);
        document.NotifyContentChanged();
    }

    public void OpenWorkspace(string root)
    {
        WorkspacePath = Path.GetFullPath(root);
        WorkspaceItems.Clear();
        var item = new WorkspaceItemViewModel(WorkspacePath);
        item.LoadChildren();
        WorkspaceItems.Add(item);
        Status = $"Workspace: {WorkspacePath}";
    }
}
