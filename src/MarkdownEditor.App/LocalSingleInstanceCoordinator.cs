using System.IO;
using System.IO.Pipes;

namespace MarkdownEditor.App;

internal sealed class LocalSingleInstanceCoordinator : IDisposable
{
    private const string MutexName = @"Local\MarkdownEditor.SorawitPhumwaree";
    private const string PipeName = "MarkdownEditor.SorawitPhumwaree";
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _cancellation = new();

    public LocalSingleInstanceCoordinator()
    {
        _mutex = new Mutex(true, MutexName, out var isPrimaryInstance);
        IsPrimaryInstance = isPrimaryInstance;
    }

    public bool IsPrimaryInstance { get; }

    public async Task ForwardPathAsync(string? path)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                await using var pipe = new NamedPipeClientStream(
                    ".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                await pipe.ConnectAsync(500);
                await using var writer = new StreamWriter(pipe) { AutoFlush = true };
                await writer.WriteLineAsync(path ?? string.Empty);
                return;
            }
            catch (TimeoutException) when (attempt < 4)
            {
                await Task.Delay(200);
            }
            catch (IOException) when (attempt < 4)
            {
                await Task.Delay(200);
            }
        }
    }

    public void StartListening(Func<string?, Task> onPathReceived) =>
        _ = ListenAsync(onPathReceived, _cancellation.Token);

    private static async Task ListenAsync(
        Func<string?, Task> onPathReceived,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                await pipe.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(pipe);
                var path = await reader.ReadLineAsync(cancellationToken);
                await onPathReceived(string.IsNullOrWhiteSpace(path) ? null : path);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException)
            {
                // A client can disappear between connecting and sending. Keep listening.
            }
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
        if (IsPrimaryInstance)
            _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
