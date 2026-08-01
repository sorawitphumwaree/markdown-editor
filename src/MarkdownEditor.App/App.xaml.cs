using System.IO;
using System.Windows;
using MarkdownEditor.Application.Abstractions;
using MarkdownEditor.Application.Documents;
using MarkdownEditor.Application.Paths;
using MarkdownEditor.App.ViewModels;
using MarkdownEditor.Domain.Documents;
using MarkdownEditor.Infrastructure.FileSystem;
using MarkdownEditor.Infrastructure.Paths;
using MarkdownEditor.Infrastructure.Persistence;
using MarkdownEditor.Infrastructure.Recovery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MarkdownEditor.App;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;
    private LocalSingleInstanceCoordinator? _singleInstance;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstance = new LocalSingleInstanceCoordinator();
        var requestedDocument = FindMarkdownPath(e.Args);
        if (!_singleInstance.IsPrimaryInstance)
        {
            await _singleInstance.ForwardPathAsync(requestedDocument);
            Shutdown();
            return;
        }

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MarkdownEditor");
        Directory.CreateDirectory(dataDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(dataDirectory, "Logs", "markdown-editor-.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog(dispose: true));
        services.AddSingleton<IDocumentFileStore, LocalDocumentFileStore>();
        services.AddSingleton<IWorkspacePathResolver, WorkspacePathResolver>();
        services.AddSingleton<ISettingsStore>(_ => new JsonSettingsStore(dataDirectory));
        services.AddSingleton<IRecoveryStore>(_ => new JsonRecoveryStore(dataDirectory));
        services.AddSingleton<DocumentWorkflow>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        _services = services.BuildServiceProvider();

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Logger.Error(args.Exception, "Unhandled UI exception");
            MessageBox.Show(args.Exception.Message, "Markdown Editor",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        var viewModel = _services.GetRequiredService<MainViewModel>();
        if (requestedDocument is null)
        {
            viewModel.NewDocument();
        }
        else
        {
            var opened = await viewModel.OpenAsync(requestedDocument);
            opened.Model.ViewMode = DocumentViewMode.Preview;
        }

        MainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow.Show();
        _singleInstance.StartListening(path => Dispatcher.InvokeAsync(
            () => HandleForwardedPathAsync(path)).Task.Unwrap());
    }

    private async Task HandleForwardedPathAsync(string? path)
    {
        if (_services is null || MainWindow is not MarkdownEditor.App.MainWindow mainWindow)
            return;

        if (FindMarkdownPath(path is null ? [] : [path]) is { } markdownPath)
        {
            var viewModel = _services.GetRequiredService<MainViewModel>();
            var opened = await viewModel.OpenAsync(markdownPath);
            opened.Model.ViewMode = DocumentViewMode.Preview;
            await mainWindow.SelectAndActivateDocumentAsync(opened);
        }

        if (mainWindow.WindowState == WindowState.Minimized)
            mainWindow.WindowState = WindowState.Normal;
        mainWindow.Show();
        mainWindow.Activate();
        mainWindow.Topmost = true;
        mainWindow.Topmost = false;
        mainWindow.Focus();
    }

    private static string? FindMarkdownPath(IEnumerable<string> arguments) =>
        arguments.Select(Path.GetFullPath).FirstOrDefault(path =>
            File.Exists(path)
            && (Path.GetExtension(path).Equals(".md", StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(path).Equals(".markdown", StringComparison.OrdinalIgnoreCase)));

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        _services?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
