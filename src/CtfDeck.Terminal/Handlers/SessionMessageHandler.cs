using System.Net.WebSockets;
using CtfDeck.Terminal.Features.Sessions;
using CtfDeck.Contracts.Transport;
using CtfDeck.Contracts.Protocols.Session;

namespace CtfDeck.Terminal.Handlers;

public class SessionMessageHandler
{
    private readonly SessionService _sessionService;
    private readonly ActiveSessionManager _activeSessionManager;

    public SessionMessageHandler(SessionService sessionService, ActiveSessionManager activeSessionManager)
    {
        _sessionService = sessionService;
        _activeSessionManager = activeSessionManager;
    }

    public async Task<bool> TryHandleAsync(
        string clientId,
        ReadOnlyMemory<byte> data,
        System.Net.WebSockets.WebSocket webSocket,
        CancellationToken cancellationToken,
        SemaphoreSlim? sendLock = null)
    {
        if (data.Length == 0) return false;

        var messageType = (MessageType)data.Span[0];
        if (!SessionProtocolDeserializer.IsSessionMessage(messageType)) return false;

        byte[] response;

        try
        {
            response = messageType switch
            {
                MessageType.SessionCreate => HandleCreate(data.Span),
                MessageType.SessionSetActive => HandleSetActive(clientId, data.Span),
                MessageType.SessionLoad => HandleLoad(data.Span),
                MessageType.SessionList => HandleList(data.Span),
                MessageType.SessionDelete => HandleDelete(data.Span),
                MessageType.SessionUpdateTargets => HandleUpdateTargets(data.Span),
                MessageType.SessionUpdate => HandleUpdate(data.Span),
                MessageType.SessionAddTarget => HandleAddTarget(data.Span),
                MessageType.SessionDeleteTarget => HandleDeleteTarget(data.Span),
                MessageType.SessionEditTarget => HandleEditTarget(data.Span),
                _ => throw new InvalidOperationException($"Unknown session message type: {messageType}")
            };
        }
        catch (Exception ex)
        {
            var msgId = data.Length >= 17 ? new Guid(data.Span.Slice(1, 16)) : Guid.Empty;
            response = SessionProtocolSerializer.SerializeError(msgId, ex.Message);
        }

        if (sendLock != null)
        {
            await sendLock.WaitAsync(cancellationToken);
            try
            {
                await webSocket.SendAsync(
                    response,
                    WebSocketMessageType.Binary,
                    true,
                    cancellationToken);
            }
            finally
            {
                sendLock.Release();
            }
        }
        else
        {
            await webSocket.SendAsync(
                response,
                WebSocketMessageType.Binary,
                true,
                cancellationToken);
        }

        return true;
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
        var sessions = _sessionService.GetAllMetadata();
        return SessionProtocolSerializer.SerializeListResult(request.MessageId, sessions);
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
        return SessionProtocolSerializer.SerializeDeleteResult(request.MessageId, true);
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
