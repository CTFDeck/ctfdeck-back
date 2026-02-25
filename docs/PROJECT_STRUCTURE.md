# Project Structure

This document provides an overview of the CTFDeck Backend repository structure, explaining the purpose of the key directories and files.

## Root Directory

- **`src/`**: Contains the source code for the platform.
- **`tests/`**: Contains the automated test suites.
- **`docs/`**: Project documentation (Guides, Policies, etc.).
- **`ctfdeck-back.sln`**: The main .NET solution file.

---

## Source Code (`src/`)

The backend is currently consolidated into a main project:

### CtfDeck.ServerWs

Entry point and WebSocket server. Composition root that wires all dependencies manually (no DI container).

- **`WebSocket/WebSocketServer.cs`**: HTTP→WS upgrade, per-client state, message routing, command dispatch.
- **`WebSocket/OutputBatcher.cs`**: Channel-based batching (8KB/5ms) for streaming command output.
- **`Program.cs`**: Entry point of the application.

### CtfDeck.Terminal

Business logic layer. Handles terminal execution, features, and message handlers.

- **`Terminal/`**: Logic for interacting with the system shell (cmd, sh, bash) and streaming output.
- **`Features/Sessions/`**: Session CRUD, active session tracking, command recording.
- **`Features/Scripts/`**: Custom script CRUD wrapper.
- **`Features/WriteUps/`**: Write-up CRUD service.
- **`Features/Media/`**: Media CRUD service.
- **`Features/Projects/`**: Project CRUD, folder management, session assignment, write-up folder assignment.
- **`Handlers/`**: Message handler pipeline (`SessionMessageHandler`, `CustomScriptMessageHandler`, `WriteUpMessageHandler`, `MediaMessageHandler`, `ProjectMessageHandler`).

### CtfDeck.Contracts

Binary protocol, DTOs, and protocol serializers/deserializers.

- **`Transport/BinaryProtocol.cs`**: `MessageType` enum, wire structs, `PooledBufferWriter`.
- **`Models/Sessions/`**: Session, HistoryEntry, SessionTarget DTOs.
- **`Models/Scripts/`**: CustomScript DTOs.
- **`Models/WriteUps/`**: WriteUp DTOs (`WriteUpDto`, `WriteUpMetadataDto`).
- **`Models/Media/`**: Media DTOs (`MediaDto`, `MediaMetadataDto`).
- **`Models/Projects/`**: Project DTOs (`ProjectDto`, `ProjectMetadataDto`, `ProjectFolderDto`).
- **`Protocols/Session/`**: Session protocol serializer/deserializer.
- **`Protocols/CustomScript/`**: Custom script protocol serializer/deserializer.
- **`Protocols/WriteUp/`**: Write-up protocol serializer/deserializer.
- **`Protocols/Media/`**: Media protocol serializer/deserializer.
- **`Protocols/Project/`**: Project protocol serializer/deserializer.

### CtfDeck.Abstractions

Port interfaces (hexagonal architecture).

- **`Ports/Sessions/ISessionRepository.cs`**
- **`Ports/Scripts/ICustomScriptRepository.cs`**
- **`Ports/WriteUps/IWriteUpRepository.cs`**
- **`Ports/Media/IMediaRepository.cs`**
- **`Ports/Projects/IProjectRepository.cs`**

### CtfDeck.Data

LiteDB persistence layer (adapter).

- **`Db/CtfDeckDbContext.cs`**: LiteDB setup. Collections: `sessions`, `customscripts`, `writeups`, `media`, `projects`.
- **`PersistenceModels/`**: Persistence models for Sessions, Scripts, WriteUps, Media, Projects.
- **`Repositories/Sessions/`**: Implements `ISessionRepository`.
- **`Repositories/Scripts/`**: Implements `ICustomScriptRepository`.
- **`Repositories/WriteUps/`**: Implements `IWriteUpRepository`.
- **`Repositories/Media/`**: Implements `IMediaRepository`.
- **`Repositories/Projects/`**: Implements `IProjectRepository`.

---

## Tests (`tests/`)

- **CtfDeck.Tests**: Unit and integration tests.
  - **`Terminal/`**: Tests for shell interaction and command parsing.
  - **`WebSocket/`**: Tests for the binary protocol, connection handling, and integration.
  - **`WriteUp/`**: Tests for write-up protocol serialization/deserialization round-trips.
  - **`Media/`**: Tests for media protocol serialization/deserialization round-trips.
  - **`Project/`**: Tests for project protocol serialization/deserialization round-trips.

---

## Documentation (`docs/`)

- **`USER.md`**: Main guide for installation and server operation.
- **`DEVELOPMENT.md`**: Entry point for developers.
- **`CONTRIBUTING.md`**: Guidelines for contributors.
- **`PROTOCOL.md`**: Deep dive into the custom binary WebSocket protocol.
- **`COMMIT_POLICY.md`**, **`BRANCHES_POLICY.md`**, etc.: Governance and workflow policies.

---

## Build and Output

- **`bin/`** and **`obj/`**: Standard .NET build output directories (gitignored).
- **`ctfdeck.db`**: LiteDB database file created at runtime to store all data (sessions, scripts, write-ups, media).
