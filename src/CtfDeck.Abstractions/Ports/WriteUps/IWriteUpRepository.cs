using CtfDeck.Contracts.Models.WriteUps;

namespace CtfDeck.Abstractions.Ports.WriteUps;

public interface IWriteUpRepository
{
    WriteUpDto Create(Guid? sessionId, string name);
    WriteUpDto? GetById(Guid id);
    (IEnumerable<WriteUpMetadataDto> Items, int TotalCount) GetBySessionId(Guid sessionId, int offset = 0, int limit = 50);
    bool Update(Guid id, string name, string content);
    bool Delete(Guid id);

    (IEnumerable<WriteUpMetadataDto> Items, int TotalCount) GetByFolderId(Guid folderId, int offset = 0, int limit = 50);
    bool SetFolderId(Guid writeUpId, Guid? folderId);
    bool SetProjectAndFolderId(Guid writeUpId, Guid? projectId, Guid? folderId);
    void ClearFolderId(Guid folderId);
    void ClearProjectId(Guid projectId);
    (IEnumerable<WriteUpMetadataDto> Items, int TotalCount) GetAllMetadata(int offset = 0, int limit = 50, bool unassignedOnly = false);
}
