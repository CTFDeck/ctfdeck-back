using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Contracts.Protocols.Tools;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Terminal.Handlers;

public class ToolMessageHandler : MessageHandlerBase
{
    private readonly IToolInstallationCoordinator _toolInstallationCoordinator;

    public ToolMessageHandler(IToolInstallationCoordinator toolInstallationCoordinator)
    {
        _toolInstallationCoordinator = toolInstallationCoordinator;
    }

    protected override bool CanHandle(MessageType type)
    {
        return type is MessageType.ToolInventoryRequest or MessageType.ToolInstallRequest;
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

            default:
                throw new InvalidOperationException($"Unsupported tool message type '{type}'.");
        }
    }

    protected override byte[] SerializeError(Guid messageId, string error)
    {
        return ToolProtocolSerializer.SerializeError(messageId, error);
    }
}
