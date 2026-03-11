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

    public WriteUpDto Create(Guid? sessionId, string name)
        => _repository.Create(sessionId, name);

    public WriteUpDto? GetById(Guid id)
        => _repository.GetById(id);

    public (List<WriteUpMetadataDto> Items, int TotalCount) GetBySessionId(Guid sessionId, int offset = 0, int limit = 50)
    {
        var result = _repository.GetBySessionId(sessionId, offset, limit);
        return (result.Items.ToList(), result.TotalCount);
    }

    public bool Update(Guid id, string name, string content)
        => _repository.Update(id, name, content);

    public bool Delete(Guid id)
        => _repository.Delete(id);

    public (List<WriteUpMetadataDto> Items, int TotalCount) GetAllMetadata(int offset = 0, int limit = 50, bool unassignedOnly = false)
    {
        var result = _repository.GetAllMetadata(offset, limit, unassignedOnly);
        return (result.Items.ToList(), result.TotalCount);
    }
}
