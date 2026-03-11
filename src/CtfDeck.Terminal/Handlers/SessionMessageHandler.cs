using CtfDeck.Terminal.Features.Sessions;
using CtfDeck.Contracts.Transport;
using CtfDeck.Contracts.Protocols.Session;

namespace CtfDeck.Terminal.Handlers;

public sealed class SessionMessageHandler : MessageHandlerBase
{
    private readonly SessionService _sessionService;
    private readonly ActiveSessionManager _activeSessionManager;

    public SessionMessageHandler(SessionService sessionService, ActiveSessionManager activeSessionManager)
    {
        _sessionService = sessionService;
        _activeSessionManager = activeSessionManager;
    }

    protected override bool CanHandle(MessageType type)
        => SessionProtocolDeserializer.IsSessionMessage(type);

    protected override byte[] SerializeError(Guid messageId, string error)
        => SessionProtocolSerializer.SerializeError(messageId, error);

    protected override byte[] Dispatch(string clientId, MessageType type, ReadOnlySpan<byte> data)
    {
        return type switch
        {
            MessageType.SessionCreate => HandleCreate(data),
            MessageType.SessionSetActive => HandleSetActive(clientId, data),
            MessageType.SessionLoad => HandleLoad(data),
            MessageType.SessionList => HandleList(data),
            MessageType.SessionDelete => HandleDelete(data),
            MessageType.SessionUpdateTargets => HandleUpdateTargets(data),
            MessageType.SessionUpdate => HandleUpdate(data),
            MessageType.SessionAddTarget => HandleAddTarget(data),
            MessageType.SessionDeleteTarget => HandleDeleteTarget(data),
            MessageType.SessionEditTarget => HandleEditTarget(data),
            _ => throw new InvalidOperationException($"Unknown session message type: {type}")
        };
    }

    private byte[] HandleCreate(ReadOnlySpan<byte> data)
    {
        var request = new SessionCreateRequest(data);
        var session = _sessionService.Create(request.Name);
        return SessionProtocolSerializer.SerializeCreateResult(request.MessageId, true, session.Id);
    }

    private byte[] HandleSetActive(string clientId, ReadOnlySpan<byte> data)
    {
        var request = new SessionSetActiveRequest(data);
        var success = _activeSessionManager.SetActiveSession(clientId, request.SessionId);
        return SessionProtocolSerializer.SerializeSetActiveResult(request.MessageId, success);
    }

    private byte[] HandleLoad(ReadOnlySpan<byte> data)
    {
        var request = new SessionLoadRequest(data);
        var session = _sessionService.GetById(request.SessionId);
        return SessionProtocolSerializer.SerializeLoadResult(request.MessageId, session != null, session);
    }

    private byte[] HandleList(ReadOnlySpan<byte> data)
    {
        var request = new SessionListRequest(data);
        var result = _sessionService.GetAllMetadata(request.Offset, request.Limit);
        return SessionProtocolSerializer.SerializeListResult(request.MessageId, result.Items, result.TotalCount);
    }

    private byte[] HandleDelete(ReadOnlySpan<byte> data)
    {
        var request = new SessionDeleteRequest(data);
        var success = _sessionService.Delete(request.SessionId);
        return SessionProtocolSerializer.SerializeDeleteResult(request.MessageId, success);
    }

    private byte[] HandleUpdateTargets(ReadOnlySpan<byte> data)
    {
        var request = new SessionUpdateTargetsRequest(data);
        _sessionService.UpdateTargets(request.SessionId, request.Targets);
        return SessionProtocolSerializer.SerializeUpdateResult(request.MessageId, true);
    }

    private byte[] HandleUpdate(ReadOnlySpan<byte> data)
    {
        var request = new SessionUpdateRequest(data);
        var success = _sessionService.Update(request.SessionId, request.Name, request.Description);
        return SessionProtocolSerializer.SerializeUpdateResult(request.MessageId, success);
    }

    private byte[] HandleAddTarget(ReadOnlySpan<byte> data)
    {
        var request = new SessionAddTargetRequest(data);
        var target = _sessionService.AddTarget(request.SessionId, request.Target);
        return SessionProtocolSerializer.SerializeAddTargetResult(
            request.MessageId, target != null, target?.Id ?? Guid.Empty);
    }

    private byte[] HandleDeleteTarget(ReadOnlySpan<byte> data)
    {
        var request = new SessionDeleteTargetRequest(data);
        var success = _sessionService.DeleteTarget(request.SessionId, request.TargetId);
        return SessionProtocolSerializer.SerializeDeleteTargetResult(request.MessageId, success);
    }

    private byte[] HandleEditTarget(ReadOnlySpan<byte> data)
    {
        var request = new SessionEditTargetRequest(data);
        var success = _sessionService.UpdateTarget(request.SessionId, request.Target);
        return SessionProtocolSerializer.SerializeEditTargetResult(request.MessageId, success);
    }
}
