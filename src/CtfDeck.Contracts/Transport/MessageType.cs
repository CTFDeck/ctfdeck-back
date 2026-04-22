namespace CtfDeck.Contracts.Transport;

/// <summary>
/// High-performance binary protocol message types
/// </summary>
public enum MessageType : byte
{
    // Terminal messages
    CompleteResponse = 0,
    StreamOutput = 1,
    StreamError = 2,
    StreamEnd = 3,
    CommandKill = 4,
    CommandKillResult = 5,
    CommandExecute = 6,
    PasswordRequest = 7,    // Server -> Client
    PasswordProvide = 8,    // Client -> Server
    CommandSignal = 9,      // Client -> Server (Ctrl+C / Ctrl+D, fire-and-forget)

    // Session requests (client → server)
    SessionCreate = 10,
    SessionSetActive = 11,
    SessionLoad = 12,
    SessionList = 13,
    SessionDelete = 14,
    SessionUpdateTargets = 15,
    SessionUpdate = 16,
    SessionAddTarget = 17,
    SessionDeleteTarget = 18,
    SessionEditTarget = 19,

    // Session responses (server → client)
    SessionCreateResult = 20,
    SessionSetActiveResult = 21,
    SessionLoadResult = 22,
    SessionListResult = 23,
    SessionDeleteResult = 24,
    SessionUpdateResult = 25,
    SessionAddTargetResult = 26,
    SessionDeleteTargetResult = 27,
    SessionEditTargetResult = 28,
    SessionOperationError = 29,

    // CustomScript requests (client → server)
    CustomScriptCreate = 30,
    CustomScriptUpdate = 31,
    CustomScriptDelete = 32,
    CustomScriptList = 33,

    // CustomScript responses (server → client)
    CustomScriptCreateResult = 40,
    CustomScriptUpdateResult = 41,
    CustomScriptDeleteResult = 42,
    CustomScriptListResult = 43,
    CustomScriptOperationError = 49,

    // WriteUp requests (client → server)
    WriteUpCreate = 50,
    WriteUpUpdate = 51,
    WriteUpDelete = 52,
    WriteUpList = 53,
    WriteUpLoad = 54,
    WriteUpMove = 55,

    // WriteUp responses (server → client)
    WriteUpCreateResult = 60,
    WriteUpUpdateResult = 61,
    WriteUpDeleteResult = 62,
    WriteUpListResult = 63,
    WriteUpLoadResult = 64,
    WriteUpMoveResult = 65,
    WriteUpOperationError = 69,

    // Media requests (client → server)
    MediaUpload = 70,
    MediaLoad = 71,
    MediaDelete = 72,
    MediaList = 73,

    // Media responses (server → client)
    MediaUploadResult = 80,
    MediaLoadResult = 81,
    MediaDeleteResult = 82,
    MediaListResult = 83,
    MediaOperationError = 89,

    // Project requests (client → server)
    ProjectCreate = 90,
    ProjectLoad = 91,
    ProjectList = 92,
    ProjectUpdate = 93,
    ProjectDelete = 94,
    ProjectAddFolder = 95,
    ProjectDeleteFolder = 96,
    ProjectRenameFolder = 97,
    ProjectAssignSession = 98,

    // Project responses (server → client)
    ProjectCreateResult = 100,
    ProjectLoadResult = 101,
    ProjectListResult = 102,
    ProjectUpdateResult = 103,
    ProjectDeleteResult = 104,
    ProjectAddFolderResult = 105,
    ProjectDeleteFolderResult = 106,
    ProjectRenameFolderResult = 107,
    ProjectAssignSessionResult = 108,
    ProjectOperationError = 111,

    // Project content queries (client → server)
    ProjectListSessions = 110,
    ProjectListWriteUps = 112,

    // Project content responses (server → client)
    ProjectListSessionsResult = 109,
    ProjectListWriteUpsResult = 113,

    // Project import/export
    ProjectExport = 114,
    ProjectExportResult = 115,
    ProjectImport = 116,
    ProjectImportResult = 117,
    ProjectListExports = 118,
    ProjectListExportsResult = 119,

    // Tool requests
    ToolInventoryRequest = 120,
    ToolInstallRequest = 121,

    // Tool responses
    ToolInventoryResult = 130,
    ToolInstallAccepted = 131,
    ToolInstallProgress = 132,
    ToolOperationError = 139,
    ToolCatalogSnapshot = 140
}
