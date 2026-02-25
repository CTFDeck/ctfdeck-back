using CtfDeck.Contracts.Models.WriteUps;

namespace CtfDeck.Abstractions.Ports.WriteUps;

public interface IWriteUpRepository
{
    WriteUpDto Create(Guid sessionId, string name);
    WriteUpDto? GetById(Guid id);
    List<WriteUpMetadataDto> GetBySessionId(Guid sessionId);
    bool Update(Guid id, string name, string content);
    bool Delete(Guid id);

    List<WriteUpMetadataDto> GetByFolderId(Guid folderId);
    bool SetFolderId(Guid writeUpId, Guid? folderId);
    void ClearFolderId(Guid folderId);
}
