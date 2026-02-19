using CtfDeck.Contracts.Transport;
using CtfDeck.Terminal.Features.Sessions;

namespace CtfDeck.ServerWs.WebSocket;

public sealed class CommandDispatcher
{
    private readonly ActiveSessionManager _activeSessionManager;
    private readonly CancellationToken _serverShutdownToken;

    public CommandDispatcher(ActiveSessionManager activeSessionManager, CancellationToken serverShutdownToken)
    {
        _activeSessionManager = activeSessionManager;
        _serverShutdownToken = serverShutdownToken;
    }

    /// <summary>
    /// Process cd command sequentially (modifies shared cwd state)
    /// </summary>
    public async Task ProcessCdAsync(ClientContext ctx, WebSocketCommand command)
    {
        Console.WriteLine($"Client {ctx.ClientId} sent command: '{command.Command}' (ID: {command.MessageId})");

        var result = await ctx.Executor.ExecuteAsync(command.Command);

        var response = WebSocketResponse.FromResult(
            result.ExitCode, result.Output, result.Error,
            result.WorkingDirectory, command.MessageId);

        await ctx.Sender.SendAsync(response.Serialize());

        _activeSessionManager.RecordCommand(
            ctx.ClientId, command.Command, result.Output + result.Error,
            result.ExitCode, result.WorkingDirectory);
    }

    /// <summary>
    /// Process streaming command in background (enables parallel execution)
    /// </summary>
    public async Task ProcessStreamingAsync(ClientContext ctx, WebSocketCommand command)
    {
        var clientId = ctx.ClientId;
        var executor = ctx.Executor;

        Console.WriteLine($"Client {clientId} sent command: '{command.Command}' (ID: {command.MessageId})");

        // Create a linked CancellationTokenSource for this command
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_serverShutdownToken);

        // Register in active commands for kill support
        ctx.ActiveCommands.TryAdd(command.MessageId, cts);

        try
        {
            var trimmed = command.Command.TrimStart();

            string? sudoPassword = null;

            // Request sudo password if needed
            if (trimmed.StartsWith("sudo ", StringComparison.Ordinal) || trimmed == "sudo")
            {
                sudoPassword = await RequestSudoPasswordAsync(ctx, command.MessageId, cts.Token);
                if (string.IsNullOrEmpty(sudoPassword))
                {
                    throw new OperationCanceledException("Sudo password not provided");
                }
            }

            await using var batcher = new OutputBatcher(ctx.Sender, command.MessageId);

            var streamResult = await executor.ExecuteStreamingAsync(
                command.Command,
                (data, isError) => batcher.EnqueueAsync(data, isError).AsTask(),
                cts.Token,
                sudoPassword);

            var accumulatedOutput = await batcher.CompleteAsync(streamResult.ExitCode, executor.CurrentDirectory);

            _activeSessionManager.RecordCommand(
                clientId, command.Command, accumulatedOutput,
                streamResult.ExitCode, executor.CurrentDirectory);
        }
        catch (OperationCanceledException)
        {
            // Command was killed or sudo password cancelled — send StreamEnd with exitCode -1
            try
            {
                var endData = BinaryProtocolSerializer.SerializeStreamEnd(command.MessageId, -1, executor.CurrentDirectory);
                await ctx.Sender.SendAsync(endData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending kill stream end for client {clientId}: {ex.Message}");
            }

            _activeSessionManager.RecordCommand(
                clientId, command.Command, "[killed]", -1, executor.CurrentDirectory);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command for client {clientId}: {ex.Message}");
        }
        finally
        {
            ctx.ActiveCommands.TryRemove(command.MessageId, out _);
        }
    }

    /// <summary>
    /// Handle CommandKill message — cancel a running command by its messageId
    /// </summary>
    public async Task HandleKillAsync(ClientContext ctx, Guid commandId)
    {
        var success = false;

        if (ctx.ActiveCommands.TryGetValue(commandId, out var cts))
        {
            try
            {
                cts.Cancel();
                success = true;
            }
            catch (ObjectDisposedException)
            {
                // Command already finished and disposed its CTS
            }
        }

        Console.WriteLine($"[KILL] {commandId} → {(success ? "cancelled" : "not found")}");

        var killResult = BinaryProtocolSerializer.SerializeCommandKillResult(commandId, success);
        await ctx.Sender.SendAsync(killResult);
    }

    /// <summary>
    /// Request sudo password from client and wait for response
    /// </summary>
    public async Task<string?> RequestSudoPasswordAsync(ClientContext ctx, Guid messageId, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        ctx.SudoWaiters[messageId] = tcs;

        var req = BinaryProtocolSerializer.SerializePasswordRequest(messageId, "Sudo password required");
        await ctx.Sender.SendAsync(req);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, linked.Token));
        }
        catch
        {
            // ignored
        }

        ctx.SudoWaiters.TryRemove(messageId, out _);
        return tcs.Task.IsCompleted ? tcs.Task.Result : null;
    }
}
