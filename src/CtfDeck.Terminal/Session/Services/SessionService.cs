using SessionModel = CtfDeck.Terminal.Session.Models.Session;
using CtfDeck.Terminal.Session.Models;
using CtfDeck.Terminal.Session.Repositories;

namespace CtfDeck.Terminal.Session.Services;

public class SessionService
{
    private readonly ISessionRepository _repository;

    public const int MaxOutputBytes = 10 * 1024; // 10KB
    public const string TruncatedSuffix = "\n[truncated]";

    public SessionService(ISessionRepository repository)
    {
        _repository = repository;
    }

    public SessionModel Create(string name)
    {
        return _repository.Create(name);
    }

    public SessionModel? GetById(Guid id)
    {
        return _repository.GetById(id);
    }

    public IEnumerable<SessionMetadata> GetAllMetadata()
    {
        return _repository.GetAllMetadata();
    }

    public bool Update(Guid id, string name, string description)
    {
        return _repository.Update(id, name, description);
    }

    public bool Delete(Guid id)
    {
        return _repository.Delete(id);
    }

    public void AddHistoryEntry(Guid sessionId, string command, string output, int exitCode, string workingDirectory)
    {
        var truncatedOutput = TruncateOutput(output);

        var entry = new HistoryEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            WorkingDirectory = workingDirectory,
            Command = command,
            Output = truncatedOutput,
            ExitCode = exitCode
        };

        _repository.AddHistoryEntry(sessionId, entry);
    }

    public void UpdateTargets(Guid sessionId, List<SessionTarget> targets)
    {
        _repository.UpdateTargets(sessionId, targets);
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
