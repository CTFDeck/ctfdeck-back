using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Contracts.Protocols.Tools;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Terminal.Handlers;

public class ToolMessageHandler : MessageHandlerBase
{
    private readonly IToolInstallationCoordinator _toolInstallationCoordinator;
    private readonly Func<string, string, CancellationToken, Task<string?>>? _requestSecretAsync;

    public ToolMessageHandler(
        IToolInstallationCoordinator toolInstallationCoordinator,
        Func<string, string, CancellationToken, Task<string?>>? requestSecretAsync = null)
    {
        _toolInstallationCoordinator = toolInstallationCoordinator;
        _requestSecretAsync = requestSecretAsync;
    }

    protected override bool CanHandle(MessageType type)
    {
        return type is MessageType.ToolInventoryRequest or MessageType.ToolInstallRequest or MessageType.ToolUninstallRequest;
    }

    protected override byte[] Dispatch(string clientId, MessageType type, ReadOnlySpan<byte> data)
    {
        switch (type)
        {
            case MessageType.ToolInventoryRequest:
                {
                    var request = new ToolInventoryRequest(data);
                    var messageId = request.MessageId;

                    var inventory = _toolInstallationCoordinator
                        .GetInventoryAsync(CurrentCancellationToken)
                        .GetAwaiter()
                        .GetResult();

                    return ToolProtocolSerializer.SerializeInventoryResult(messageId, inventory);
                }

            case MessageType.ToolInstallRequest:
                {
                    var request = new ToolInstallRequest(data);
                    var messageId = request.MessageId;
                    var toolIds = request.ToolIds.ToArray();
                    var sendAsync = CurrentSendAsync
                        ?? throw new InvalidOperationException("CurrentSendAsync is not available.");

                    var cancellationToken = CurrentCancellationToken;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _toolInstallationCoordinator.InstallAsync(
                                toolIds,
                                async progress =>
                                {
                                    await sendAsync(
                                        ToolProtocolSerializer.SerializeInstallProgress(messageId, progress));
                                },
                                async (prompt, ct) =>
                                {
                                    if (_requestSecretAsync is null)
                                        return null;

                                    return await _requestSecretAsync(clientId, prompt, ct);
                                },
                                cancellationToken);

                            var inventory = await _toolInstallationCoordinator.GetInventoryAsync(cancellationToken);

                            await sendAsync(
                                ToolProtocolSerializer.SerializeInventoryResult(messageId, inventory));
                        }
                        catch (Exception ex)
                        {
                            await sendAsync(
                                ToolProtocolSerializer.SerializeError(messageId, ex.Message));
                        }
                    }, cancellationToken);

                    return ToolProtocolSerializer.SerializeInstallAccepted(messageId, true);
                }

            case MessageType.ToolUninstallRequest:
                {
                    var request = new ToolUninstallRequest(data);
                    var messageId = request.MessageId;
                    var toolIds = request.ToolIds.ToArray();
                    var sendAsync = CurrentSendAsync
                        ?? throw new InvalidOperationException("CurrentSendAsync is not available.");

                    var cancellationToken = CurrentCancellationToken;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _toolInstallationCoordinator.UninstallAsync(
                                toolIds,
                                async progress =>
                                {
                                    await sendAsync(
                                        ToolProtocolSerializer.SerializeInstallProgress(messageId, progress));
                                },
                                async (prompt, ct) =>
                                {
                                    if (_requestSecretAsync is null)
                                        return null;

                                    return await _requestSecretAsync(clientId, prompt, ct);
                                },
                                cancellationToken);

                            var inventory = await _toolInstallationCoordinator.GetInventoryAsync(cancellationToken);

                            await sendAsync(
                                ToolProtocolSerializer.SerializeInventoryResult(messageId, inventory));
                        }
                        catch (Exception ex)
                        {
                            await sendAsync(
                                ToolProtocolSerializer.SerializeError(messageId, ex.Message));
                        }
                    }, cancellationToken);

                    return ToolProtocolSerializer.SerializeUninstallAccepted(messageId, true);
                }

            default:
                throw new InvalidOperationException($"Unsupported tool message type '{type}'.");
        }
    }

    protected override byte[] SerializeError(Guid messageId, string error)
    {
        return ToolProtocolSerializer.SerializeError(messageId, error);
    }
}
