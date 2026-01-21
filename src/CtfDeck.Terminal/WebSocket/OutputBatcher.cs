using System.Threading.Channels;
using System.Net.WebSockets;
using System.Text;

namespace CtfDeck.Terminal.WebSocket;

/// <summary>
/// High-performance output batcher that coalesces rapid outputs into fewer WebSocket sends.
/// Uses a channel-based producer-consumer pattern for maximum throughput.
/// </summary>
public sealed class OutputBatcher : IAsyncDisposable
{
    private readonly System.Net.WebSockets.WebSocket _socket;
    private readonly Guid _messageId;
    private readonly Channel<(string Data, bool IsError)> _channel;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts;

    // Batching configuration
    private const int MaxBatchSize = 8192;  // 8KB max before force-send
    private const int BatchDelayMs = 5;      // 5ms max delay for batching (200Hz)

    public OutputBatcher(System.Net.WebSockets.WebSocket socket, Guid messageId)
    {
        _socket = socket;
        _messageId = messageId;
        _cts = new CancellationTokenSource();

        // Unbounded channel for maximum throughput
        _channel = Channel.CreateUnbounded<(string, bool)>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        });

        _processingTask = ProcessBatchesAsync(_cts.Token);
    }

    /// <summary>
    /// Enqueue output for batched sending
    /// </summary>
    public ValueTask EnqueueAsync(string data, bool isError)
    {
        return _channel.Writer.TryWrite((data, isError))
            ? ValueTask.CompletedTask
            : _channel.Writer.WriteAsync((data, isError));
    }

    private async Task ProcessBatchesAsync(CancellationToken ct)
    {
        var stdoutBatch = new StringBuilder(MaxBatchSize);
        var stderrBatch = new StringBuilder(MaxBatchSize);

        try
        {
            while (await _channel.Reader.WaitToReadAsync(ct))
            {
                stdoutBatch.Clear();
                stderrBatch.Clear();

                if (await CollectBatchAsync(stdoutBatch, stderrBatch, ct))
                {
                    await FlushRemainingAsync(stdoutBatch, stderrBatch, ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (WebSocketException)
        {
            // Client disconnected
        }
    }

    private async Task<bool> CollectBatchAsync(StringBuilder stdout, StringBuilder stderr, CancellationToken ct)
    {
        using var batchCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        batchCts.CancelAfter(BatchDelayMs);

        try
        {
            // Initial consumption of whatever is ready
            while (_channel.Reader.TryRead(out var item))
            {
                 AppendToBatch(item.Data, item.IsError, stdout, stderr, ct);
            }

            // Wait for more until timeout
            while (!batchCts.Token.IsCancellationRequested)
            {
                // Wait for data or timeout
                if (!await _channel.Reader.WaitToReadAsync(batchCts.Token))
                {
                   return true; // Channel closed, process what we have
                }

                while (_channel.Reader.TryRead(out var item))
                {
                    AppendToBatch(item.Data, item.IsError, stdout, stderr, ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
             // Batch timeout or main cancellation
        }

        return true;
    }

    private void AppendToBatch(string data, bool isError, StringBuilder stdout, StringBuilder stderr, CancellationToken ct)
    {
        var batch = isError ? stderr : stdout;
        batch.Append(data);

        if (batch.Length >= MaxBatchSize)
        {
            _ = FlushBatchAsync(batch, isError, ct);
            batch.Clear();
        }
    }

    private async Task FlushRemainingAsync(StringBuilder stdout, StringBuilder stderr, CancellationToken ct)
    {
        if (stdout.Length > 0) await FlushBatchAsync(stdout, false, ct);
        if (stderr.Length > 0) await FlushBatchAsync(stderr, true, ct);
    }

    private async Task FlushBatchAsync(StringBuilder batch, bool isError, CancellationToken ct)
    {
        if (batch.Length == 0 || _socket.State != WebSocketState.Open)
            return;

        var messageType = isError ? MessageType.StreamError : MessageType.StreamOutput;
        var data = BinaryProtocolSerializer.SerializeStreamChunk(messageType, _messageId, batch.ToString());

        await _socket.SendAsync(data, WebSocketMessageType.Binary, true, ct);
    }

    /// <summary>
    /// Complete batching and send stream end message
    /// </summary>
    public async Task CompleteAsync(int exitCode, string workingDirectory)
    {
        _channel.Writer.Complete();
        await _processingTask;

        if (_socket.State == WebSocketState.Open)
        {
            var data = BinaryProtocolSerializer.SerializeStreamEnd(_messageId, exitCode, workingDirectory);
            await _socket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _channel.Writer.TryComplete();

        try
        {
            await _processingTask;
        }
        catch (OperationCanceledException) { }

        _cts.Dispose();
    }
}
