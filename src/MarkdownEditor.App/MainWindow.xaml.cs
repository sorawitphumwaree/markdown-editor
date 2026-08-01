using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MarkdownEditor.Application.Messaging;
using MarkdownEditor.Application.Paths;
using MarkdownEditor.App.ViewModels;
using MarkdownEditor.Domain.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;

namespace MarkdownEditor.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IWorkspacePathResolver _pathResolver;
    private readonly ILogger<MainWindow> _logger;
    private bool _webReady;
    private bool _synchronizingViewToggle;
    private TaskCompletionSource<bool>? _exportReady;

    public MainWindow(
        MainViewModel viewModel,
        IWorkspacePathResolver pathResolver,
        ILogger<MainWindow> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _pathResolver = pathResolver;
        _logger = logger;
        DataContext = viewModel;
        Loaded += InitializeWebView;
    }

    private async void InitializeWebView(object sender, RoutedEventArgs e)
    {
        try
        {
            await WebView.EnsureCoreWebView2Async();
            var webRoot = Path.Combine(AppContext.BaseDirectory, "Web");
            WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.markdown-editor", webRoot, CoreWebView2HostResourceAccessKind.DenyCors);
            WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            WebView.CoreWebView2.WebMessageReceived += WebMessageReceived;
            WebView.Source = new Uri("https://app.markdown-editor/index.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebView2 initialization failed");
            MessageBox.Show(
                "WebView2 could not start. Install the Microsoft Edge WebView2 Runtime.",
                "Markdown Editor", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        AppMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<AppMessage>(
                e.WebMessageAsJson, JsonDefaults.Options);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Rejected malformed WebView message");
            return;
        }

        if (message is null || message.ProtocolVersion != AppMessage.CurrentProtocolVersion)
            return;

        switch (message.Type)
        {
            case "web.ready":
                _webReady = true;
                await ActivateDocumentAsync();
                break;
            case "document.changed":
                if (FindDocument(message.DocumentId) is { } changed
                    && message.Version >= changed.Model.Version
                    && message.Payload.TryGetProperty("content", out var content))
                {
                    await _viewModel.UpdateContentAsync(changed, content.GetString() ?? string.Empty);
                }
                break;
            case "document.saveRequested":
                await SaveActiveAsync();
                break;
            case "editor.cursorChanged":
                if (FindDocument(message.DocumentId) is { } cursorDocument
                    && message.Payload.TryGetProperty("line", out var line)
                    && line.TryGetInt32(out var cursorLine))
                    cursorDocument.Model.CursorLine = Math.Max(1, cursorLine);
                break;
            case "editor.scrollChanged":
                if (FindDocument(message.DocumentId) is { } editorScrollDocument
                    && message.Payload.TryGetProperty("top", out var editorTop)
                    && editorTop.TryGetDouble(out var editorScroll))
                    editorScrollDocument.Model.EditorScrollPosition = Math.Max(0, editorScroll);
                break;
            case "preview.scrollChanged":
                if (FindDocument(message.DocumentId) is { } previewScrollDocument
                    && message.Payload.TryGetProperty("top", out var previewTop)
                    && previewTop.TryGetDouble(out var previewScroll))
                    previewScrollDocument.Model.PreviewScrollPosition = Math.Max(0, previewScroll);
                break;
            case "application.shortcut":
                if (message.Payload.TryGetProperty("command", out var command))
                    await HandleShortcutAsync(command.GetString());
                break;
            case "preview.linkClicked":
                await HandleLinkAsync(message.Payload);
                break;
            case "export.ready":
                _exportReady?.TrySetResult(true);
                break;
            case "application.log":
                _logger.LogInformation("Web: {Message}", message.Payload.ToString());
                break;
            default:
                _logger.LogDebug("Ignoring unknown web message {Type}", message.Type);
                break;
        }
    }

    private DocumentTabViewModel? FindDocument(Guid? id) =>
        id is null ? null : _viewModel.Documents.FirstOrDefault(item => item.Id == id);

    private async Task ActivateDocumentAsync()
    {
        if (!_webReady)
            return;
        if (_viewModel.ActiveDocument is not { } active)
        {
            await PostAsync("document.clear", null, new { });
            return;
        }
        await PostAsync("document.load", active, new
        {
            content = active.Model.Content,
            filePath = active.FilePath,
            viewMode = active.Model.ViewMode.ToString().ToLowerInvariant(),
            cursorLine = active.Model.CursorLine,
            editorScroll = active.Model.EditorScrollPosition,
            previewScroll = active.Model.PreviewScrollPosition
        });
    }

    public Task ActivateCurrentDocumentAsync() => ActivateDocumentAsync();

    public async Task SelectAndActivateDocumentAsync(DocumentTabViewModel document)
    {
        _viewModel.ActiveDocument = document;
        DocumentTabs.SelectedItem = document;
        DocumentTabs.UpdateLayout();
        if (DocumentTabs.ItemContainerGenerator.ContainerFromItem(document)
            is TabItem selectedTab)
            selectedTab.BringIntoView();
        await ActivateDocumentAsync();
    }

    private Task PostAsync(
        string type,
        DocumentTabViewModel? document,
        object payload)
    {
        var message = new
        {
            type,
            protocolVersion = AppMessage.CurrentProtocolVersion,
            documentId = document?.Id,
            version = document?.Model.Version,
            payload
        };
        WebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, JsonDefaults.Options));
        return Task.CompletedTask;
    }

    private async Task HandleLinkAsync(JsonElement payload)
    {
        if (_viewModel.ActiveDocument is not { } active
            || !payload.TryGetProperty("href", out var hrefProperty))
            return;

        var href = hrefProperty.GetString();
        if (string.IsNullOrWhiteSpace(href))
            return;

        var result = _pathResolver.Resolve(active.FilePath, href, _viewModel.WorkspacePath);
        switch (result.Kind)
        {
            case PathTargetKind.Markdown:
                await _viewModel.OpenAsync(result.FullPath!);
                await ActivateDocumentAsync();
                if (result.Fragment is not null)
                    await PostAsync("preview.navigateFragment", _viewModel.ActiveDocument,
                        new { fragment = result.Fragment });
                break;
            case PathTargetKind.ExternalUrl:
            case PathTargetKind.OtherLocalFile:
                Process.Start(new ProcessStartInfo(result.FullPath!) { UseShellExecute = true });
                break;
            default:
                MessageBox.Show(result.Error ?? "The link target cannot be opened.",
                    "Markdown Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
                break;
        }
    }

    private async void OpenFile(object sender, RoutedEventArgs e) => await OpenFileAsync();

    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Markdown files (*.md;*.markdown)|*.md;*.markdown|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == true)
        {
            await _viewModel.OpenAsync(dialog.FileName);
            await ActivateDocumentAsync();
        }
    }

    private async void NewFile(object sender, RoutedEventArgs e)
    {
        _viewModel.NewDocument();
        await ActivateDocumentAsync();
    }

    private void OpenFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog(this) == true)
            _viewModel.OpenWorkspace(dialog.FolderName);
    }

    private async void SaveFile(object sender, RoutedEventArgs e) => await SaveActiveAsync();

    private async Task SaveActiveAsync()
    {
        if (_viewModel.ActiveDocument is not { } active)
            return;
        if (!active.Model.ExistsOnDisk)
        {
            await SaveAsActiveAsync();
            return;
        }
        await _viewModel.SaveAsync(active);
    }

    private async void SaveAsFile(object sender, RoutedEventArgs e)
        => await SaveAsActiveAsync();

    private async Task<bool> SaveAsActiveAsync()
    {
        if (_viewModel.ActiveDocument is not { } active)
            return false;
        var dialog = new SaveFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
            FileName = active.DisplayName
        };
        if (dialog.ShowDialog(this) != true)
            return false;
        await _viewModel.SaveAsync(active, dialog.FileName);
        return true;
    }

    private async void ExportPdf(object sender, RoutedEventArgs e)
    {
        if (_viewModel.ActiveDocument is { } active)
            await ExportDocumentAsync(active);
    }

    private async Task ExportDocumentAsync(DocumentTabViewModel document)
    {
        if (!_webReady)
            return;
        _viewModel.ActiveDocument = document;
        await ActivateDocumentAsync();
        var dialog = new SaveFileDialog { Filter = "PDF document (*.pdf)|*.pdf", FileName = "document.pdf" };
        if (dialog.ShowDialog(this) != true)
            return;
        try
        {
            _exportReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
            await PostAsync("export.prepare", document, new { theme = "light" });
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await _exportReady.Task.WaitAsync(timeout.Token);
            var settings = WebView.CoreWebView2.Environment.CreatePrintSettings();
            settings.ShouldPrintBackgrounds = true;
            if (!await WebView.CoreWebView2.PrintToPdfAsync(dialog.FileName, settings))
                throw new InvalidOperationException("WebView2 did not create the PDF.");
            _viewModel.Status = $"Exported {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF export failed");
            MessageBox.Show(ex.Message, "PDF export", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _exportReady = null;
            await PostAsync("export.complete", document, new { });
        }
    }

    private async void SelectPreviewView(object sender, RoutedEventArgs e)
    {
        if (_synchronizingViewToggle)
            return;
        await SetModeAsync("preview");
        SynchronizeViewToggle();
    }

    private async void SelectSplitView(object sender, RoutedEventArgs e)
    {
        if (_synchronizingViewToggle)
            return;
        await SetModeAsync("split");
        SynchronizeViewToggle();
    }

    private async Task SetModeAsync(string mode)
    {
        if (_viewModel.ActiveDocument is { } active)
        {
            active.Model.ViewMode = mode == "preview"
                ? DocumentViewMode.Preview
                : DocumentViewMode.Split;
            await PostAsync("view.setMode", active, new { mode });
        }
    }

    private void SynchronizeViewToggle()
    {
        _synchronizingViewToggle = true;
        var split = _viewModel.ActiveDocument?.Model.ViewMode == DocumentViewMode.Split;
        PreviewModeButton.IsChecked = !split;
        SplitModeButton.IsChecked = split;
        _synchronizingViewToggle = false;
    }

    private DocumentTabViewModel? GetTabFromSender(object sender) =>
        (sender as FrameworkElement)?.DataContext as DocumentTabViewModel;

    private async void TabSave(object sender, RoutedEventArgs e)
    {
        if (GetTabFromSender(sender) is not { } document)
            return;
        _viewModel.ActiveDocument = document;
        await ActivateDocumentAsync();
        await SaveActiveAsync();
    }

    private async void TabExportPdf(object sender, RoutedEventArgs e)
    {
        if (GetTabFromSender(sender) is { } document)
            await ExportDocumentAsync(document);
    }

    private async void TabReload(object sender, RoutedEventArgs e)
    {
        if (GetTabFromSender(sender) is not { } document || !document.Model.ExistsOnDisk)
            return;
        if (document.Model.IsModified
            && MessageBox.Show(
                $"Discard unsaved changes and reload {document.DisplayName} from disk?",
                "Reload from Disk", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes)
            return;
        try
        {
            _viewModel.ActiveDocument = document;
            await _viewModel.ReloadAsync(document);
            await ActivateDocumentAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reload failed for {Path}", document.FilePath);
            MessageBox.Show(ex.Message, "Reload from Disk", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TabClose(object sender, RoutedEventArgs e)
    {
        if (GetTabFromSender(sender) is not { } document)
            return;
        _viewModel.ActiveDocument = document;
        await CloseActiveTabAsync();
    }

    private void OpenHelpReference(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string url })
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void ShowAbout(object sender, RoutedEventArgs e) =>
        new AboutWindow { Owner = this }.ShowDialog();

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            return;

        var command = e.Key switch
        {
            Key.Tab when (Keyboard.Modifiers & ModifierKeys.Shift) != 0 => "previousTab",
            Key.Tab => "nextTab",
            Key.W => "closeTab",
            Key.T or Key.N => "newTab",
            Key.O => "open",
            Key.S when (Keyboard.Modifiers & ModifierKeys.Shift) != 0 => "saveAs",
            Key.S => "save",
            _ => null
        };
        if (command is null)
            return;
        e.Handled = true;
        await HandleShortcutAsync(command);
    }

    private async Task HandleShortcutAsync(string? command)
    {
        switch (command)
        {
            case "nextTab":
                SelectRelativeTab(1);
                break;
            case "previousTab":
                SelectRelativeTab(-1);
                break;
            case "closeTab":
                await CloseActiveTabAsync();
                break;
            case "newTab":
                _viewModel.NewDocument();
                await ActivateDocumentAsync();
                break;
            case "save":
                await SaveActiveAsync();
                break;
            case "saveAs":
                await SaveAsActiveAsync();
                break;
            case "open":
                await OpenFileAsync();
                break;
        }
    }

    private void SelectRelativeTab(int offset)
    {
        if (_viewModel.Documents.Count < 2 || _viewModel.ActiveDocument is null)
            return;
        var current = _viewModel.Documents.IndexOf(_viewModel.ActiveDocument);
        var next = (current + offset + _viewModel.Documents.Count) % _viewModel.Documents.Count;
        _viewModel.ActiveDocument = _viewModel.Documents[next];
    }

    private async Task CloseActiveTabAsync()
    {
        if (_viewModel.ActiveDocument is not { } active)
            return;
        if (active.Model.IsModified)
        {
            var result = MessageBox.Show(
                $"Save changes to {active.DisplayName}?",
                "Markdown Editor", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Cancel)
                return;
            if (result == MessageBoxResult.Yes)
            {
                if (!active.Model.ExistsOnDisk && !await SaveAsActiveAsync())
                    return;
                if (active.Model.ExistsOnDisk && active.Model.IsModified)
                    await _viewModel.SaveAsync(active);
            }
        }
        _viewModel.Close(active);
        await ActivateDocumentAsync();
    }

    private async void ActiveTabChanged(object sender, SelectionChangedEventArgs e)
    {
        SynchronizeViewToggle();
        await ActivateDocumentAsync();
    }

    private void WorkspaceItemExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem { DataContext: WorkspaceItemViewModel item })
            item.LoadChildren();
    }

    private async void WorkspaceItemDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if ((sender as TreeView)?.SelectedItem is WorkspaceItemViewModel { IsDirectory: false } item
            && Path.GetExtension(item.Path) is ".md" or ".markdown")
        {
            await _viewModel.OpenAsync(item.Path);
            await ActivateDocumentAsync();
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        var modified = _viewModel.Documents.Where(item => item.Model.IsModified).ToArray();
        if (modified.Length == 0)
            return;
        var result = MessageBox.Show(
            $"{modified.Length} document(s) have unsaved changes. Exit anyway?",
            "Markdown Editor", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        e.Cancel = result != MessageBoxResult.Yes;
    }

    private void Exit(object sender, RoutedEventArgs e) => Close();

    private static class JsonDefaults
    {
        public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);
    }
}
