using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Abstractions.Ports.WriteUps;

namespace CtfDeck.Terminal.Features.WriteUps;

public class WriteUpService
{
    private readonly IWriteUpRepository _repository;

    public WriteUpService(IWriteUpRepository repository)
    {
        _repository = repository;
    }

    public WriteUpDto Create(Guid sessionId, string name)
        => _repository.Create(sessionId, name);

    public WriteUpDto? GetById(Guid id)
        => _repository.GetById(id);

    public List<WriteUpMetadataDto> GetBySessionId(Guid sessionId)
        => _repository.GetBySessionId(sessionId);

    public bool Update(Guid id, string name, string content)
        => _repository.Update(id, name, content);

    public bool Delete(Guid id)
        => _repository.Delete(id);
}
