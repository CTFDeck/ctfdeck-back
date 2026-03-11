using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Abstractions.Ports.Sessions;

namespace CtfDeck.Terminal.Features.Sessions;

public class SessionService
{
    private readonly ISessionRepository _repository;

    public const int MaxOutputBytes = 10 * 1024; // 10KB
    public const string TruncatedSuffix = "\n[truncated]";

    public SessionService(ISessionRepository repository)
    {
        _repository = repository;
    }

    public SessionDto Create(string name)
        => _repository.Create(name);

    public SessionDto? GetById(Guid id)
        => _repository.GetById(id);

    public (IEnumerable<SessionMetadataDto> Items, int TotalCount) GetAllMetadata(int offset = 0, int limit = 50, bool unassignedOnly = false)
        => _repository.GetAllMetadata(offset, limit, unassignedOnly);

    public bool Update(Guid id, string name, string description)
        => _repository.Update(id, name, description);

    public bool Delete(Guid id)
        => _repository.Delete(id);

    public void AddHistoryEntry(Guid sessionId, string command, string output, int exitCode, string workingDirectory)
    {
        var entry = new HistoryEntryDto
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            WorkingDirectory = workingDirectory,
            Command = command,
            Output = TruncateOutput(output),
            ExitCode = exitCode
        };

        _repository.AddHistoryEntry(sessionId, entry);
    }

    public void UpdateTargets(Guid sessionId, List<SessionTargetDto> targets)
        => _repository.UpdateTargets(sessionId, targets);

    public SessionTargetDto? AddTarget(Guid sessionId, SessionTargetDto target)
    {
        target.Description = AutoFillDescription(target.Description, target.Name, target.Address, target.Port);
        return _repository.AddTarget(sessionId, target);
    }

    public bool DeleteTarget(Guid sessionId, Guid targetId)
        => _repository.DeleteTarget(sessionId, targetId);

    public bool UpdateTarget(Guid sessionId, SessionTargetDto target)
    {
        target.Description = AutoFillDescription(target.Description, target.Name, target.Address, target.Port);
        return _repository.UpdateTarget(sessionId, target);
    }

    private static string AutoFillDescription(string description, string name, string address, int? port)
    {
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        return port.HasValue ? $"{name} {address}:{port}" : $"{name} {address}";
    }

    private static string TruncateOutput(string output)
    {
        if (string.IsNullOrEmpty(output)) return string.Empty;

        var byteCount = System.Text.Encoding.UTF8.GetByteCount(output);
        if (byteCount <= MaxOutputBytes) return output;

        var suffixBytes = System.Text.Encoding.UTF8.GetByteCount(TruncatedSuffix);
        var targetBytes = MaxOutputBytes - suffixBytes;

        var chars = output.AsSpan();
        var currentBytes = 0;
        var charIndex = 0;

        while (charIndex < chars.Length)
        {
            var charBytes = System.Text.Encoding.UTF8.GetByteCount(chars.Slice(charIndex, 1));
            if (currentBytes + charBytes > targetBytes) break;
            currentBytes += charBytes;
            charIndex++;
        }

        return output[..charIndex] + TruncatedSuffix;
    }
}
