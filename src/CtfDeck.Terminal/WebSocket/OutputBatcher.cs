using System.Threading.Channels;
using System.Net.WebSockets;

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
        var reader = _channel.Reader;
        var stdoutBatch = new System.Text.StringBuilder(MaxBatchSize);
        var stderrBatch = new System.Text.StringBuilder(MaxBatchSize);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Wait for first item
                if (!await reader.WaitToReadAsync(ct))
                    break;

                stdoutBatch.Clear();
                stderrBatch.Clear();

                // Collect items for batch (with timeout)
                using var batchCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                batchCts.CancelAfter(BatchDelayMs);

                try
                {
                    while (reader.TryRead(out var item))
                    {
                        var batch = item.IsError ? stderrBatch : stdoutBatch;
                        batch.Append(item.Data);

                        // Force send if batch is large
                        if (batch.Length >= MaxBatchSize)
                        {
                            await FlushBatchAsync(batch, item.IsError, ct);
                            batch.Clear();
                        }
                    }

                    // Wait a tiny bit more for additional items
                    while (!batchCts.Token.IsCancellationRequested &&
                           await reader.WaitToReadAsync(batchCts.Token))
                    {
                        while (reader.TryRead(out var item))
                        {
                            var batch = item.IsError ? stderrBatch : stdoutBatch;
                            batch.Append(item.Data);

                            if (batch.Length >= MaxBatchSize)
                            {
                                await FlushBatchAsync(batch, item.IsError, ct);
                                batch.Clear();
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (batchCts.Token.IsCancellationRequested)
                {
                    // Batch timeout - flush what we have
                }

                // Send any remaining batched data
                if (stdoutBatch.Length > 0)
                    await FlushBatchAsync(stdoutBatch, false, ct);
                if (stderrBatch.Length > 0)
                    await FlushBatchAsync(stderrBatch, true, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { }
    }

    private async Task FlushBatchAsync(System.Text.StringBuilder batch, bool isError, CancellationToken ct)
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

        try
        {
            // Wait for processing to finish
            await _processingTask;
        }
        catch { }

        // Send stream end
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
        catch { }

        _cts.Dispose();
    }
}
