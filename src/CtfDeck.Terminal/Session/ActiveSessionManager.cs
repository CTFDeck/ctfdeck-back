using System.Collections.Concurrent;
using CtfDeck.Terminal.Session.Services;

namespace CtfDeck.Terminal.Session;

public class ActiveSessionManager
{
    private readonly ConcurrentDictionary<string, Guid> _activeSessions = new();
    private readonly SessionService _sessionService;

    public ActiveSessionManager(SessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public bool SetActiveSession(string clientId, Guid sessionId)
    {
        if (sessionId == Guid.Empty)
        {
            _activeSessions.TryRemove(clientId, out _);
            return true;
        }

        var session = _sessionService.GetById(sessionId);
        if (session == null) return false;

        _activeSessions[clientId] = sessionId;
        return true;
    }

    public Guid? GetActiveSession(string clientId)
    {
        return _activeSessions.TryGetValue(clientId, out var sessionId) ? sessionId : null;
    }

    public void ClearClient(string clientId)
    {
        _activeSessions.TryRemove(clientId, out _);
    }

    public void RecordCommand(string clientId, string command, string output, int exitCode, string workingDirectory)
    {
        if (!_activeSessions.TryGetValue(clientId, out var sessionId)) return;

        _sessionService.AddHistoryEntry(sessionId, command, output, exitCode, workingDirectory);
    }
}
