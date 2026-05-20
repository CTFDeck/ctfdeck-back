using CtfDeck.Contracts.Transport;
using CtfDeck.Terminal.Handlers;

namespace CtfDeck.WsServer.WebSocket;

public sealed class BinaryMessageDispatcher
{
    private readonly Func<ClientContext, WebSocketCommand, Task> _processCdAsync;
    private readonly Func<ClientContext, WebSocketCommand, Task> _processStreamingAsync;
    private readonly Func<ClientContext, Guid, Task> _handleKillAsync;
    private readonly Func<ClientContext, Guid, CommandSignalKind, Task> _handleSignalAsync;
    private readonly Func<ClientContext, Guid, string, Task> _handleInputAsync;
    private readonly IReadOnlyList<MessageHandlerBase> _messageHandlers;
    private readonly Action<string> _log;

    public BinaryMessageDispatcher(
        Func<ClientContext, WebSocketCommand, Task> processCdAsync,
        Func<ClientContext, WebSocketCommand, Task> processStreamingAsync,
        Func<ClientContext, Guid, Task> handleKillAsync,
        Func<ClientContext, Guid, CommandSignalKind, Task> handleSignalAsync,
        Func<ClientContext, Guid, string, Task> handleInputAsync,
        IReadOnlyList<MessageHandlerBase> messageHandlers,
        Action<string>? log = null)
    {
        _processCdAsync = processCdAsync;
        _processStreamingAsync = processStreamingAsync;
        _handleKillAsync = handleKillAsync;
        _handleSignalAsync = handleSignalAsync;
        _handleInputAsync = handleInputAsync;
        _messageHandlers = messageHandlers;
        _log = log ?? Console.WriteLine;
    }

    public async Task DispatchAsync(ClientContext ctx, ReadOnlyMemory<byte> message, CancellationToken cancellationToken)
    {
        var clientId = ctx.ClientId;
        var protocolType = (MessageType)message.Span[0];

        switch (protocolType)
        {
            case MessageType.CommandExecute:
                {
                    var messageDataCopy = message.ToArray();
                    var command = WebSocketCommand.Deserialize(messageDataCopy);
                    var trimmedCommand = command.Command.Trim();

                    if (trimmedCommand.StartsWith("cd ", StringComparison.Ordinal) || trimmedCommand == "cd")
                    {
                        await _processCdAsync(ctx, command);
                    }
                    else
                    {
                        _ = Task.Run(() => _processStreamingAsync(ctx, command), cancellationToken);
                    }

                    break;
                }

            case MessageType.CommandKill:
                {
                    var killCommandId = new CommandKillReader(message.Span).CommandId;
                    await _handleKillAsync(ctx, killCommandId);
                    break;
                }

            case MessageType.CommandSignal:
                {
                    var (commandId, kind) = ParseCommandSignal(message.Span);
                    await _handleSignalAsync(ctx, commandId, kind);
                    break;
                }

            case MessageType.CommandInput:
                {
                    var (messageId, input) = ParseCommandInput(message.Span);
                    await _handleInputAsync(ctx, messageId, input);
                    break;
                }

            case MessageType.PasswordProvide:
                {
                    var (messageId, password) = ParsePasswordProvide(message.Span);

                    if (ctx.SudoWaiters.TryRemove(messageId, out var waiter))
                    {
                        waiter.TrySetResult(password);
                    }

                    break;
                }

            default:
                {
                    var handled = false;

                    foreach (var handler in _messageHandlers)
                    {
                        if (await handler.TryHandleAsync(
                                clientId,
                                message,
                                payload => ctx.Sender.SendAsync(payload, cancellationToken),
                                cancellationToken))
                        {
                            handled = true;
                            break;
                        }
                    }

                    if (!handled)
                    {
                        _log($"Unknown message from client {clientId}: type={message.Span[0]}, size={message.Length}");
                    }

                    break;
                }
        }
    }

    private static (Guid CommandId, CommandSignalKind Kind) ParseCommandSignal(ReadOnlySpan<byte> span)
    {
        var signalReader = new CommandSignalReader(span);
        return (signalReader.CommandId, signalReader.Kind);
    }

    private static (Guid MessageId, string Input) ParseCommandInput(ReadOnlySpan<byte> span)
    {
        var inputReader = new CommandInputReader(span);
        return (inputReader.MessageId, inputReader.Input);
    }

    private static (Guid MessageId, string? Password) ParsePasswordProvide(ReadOnlySpan<byte> span)
    {
        var reader = new PasswordProvideReader(span);
        var password = reader.PasswordLength == 0 ? null : reader.GetPassword();
        return (reader.MessageId, password);
    }
}
