# Database Schema

## Overview

CtfDeck uses **LiteDB** (v5.0.21), an embedded NoSQL document database for .NET. Data is stored in a single file with no external dependencies.

### Database Location
- **Production**: `ctfdeck_sessions.db` (same folder as executable)
- **Tests**: In-memory database (`:memory:`)

---

## Collections

### `sessions`

Main collection storing all session documents.

**Indexes:**
| Field | Type | Unique |
|-------|------|--------|
| `_id` (Id) | GUID | Yes |
| `Name` | String | No |
| `ProjectId` | GUID | No |

### `customscripts`

Collection storing reusable command templates (custom scripts).

**Indexes:**
| Field | Type | Unique |
|-------|------|--------|
| `_id` (Id) | GUID | Yes |

### `writeups`

Collection storing CTF write-ups (reports). Each write-up is linked to a session via `SessionId`.

**Indexes:**
| Field | Type | Unique |
|-------|------|--------|
| `_id` (Id) | GUID | Yes |
| `SessionId` | GUID | No |
| `FolderId` | GUID | No |

### `media`

Collection storing binary blobs (images, videos, PDFs, etc.) that can be referenced from write-up markdown content.

**Indexes:**
| Field | Type | Unique |
|-------|------|--------|
| `_id` (Id) | GUID | Yes |
| `FileName` | String | No |

### `projects`

Collection storing CTF projects. Projects group sessions and write-up folders. Folders are embedded in the project document.

**Indexes:**
| Field | Type | Unique |
|-------|------|--------|
| `_id` (Id) | GUID | Yes |
| `Name` | String | No |

---

## Document Schemas

### Session

Root document representing a CTF session.

```json
{
  "_id": "GUID",
  "Name": "string",
  "Description": "string",
  "CreatedAt": "DateTime (UTC)",
  "UpdatedAt": "DateTime (UTC)",
  "ProjectId": "GUID | null",
  "History": [HistoryEntry],
  "Targets": [SessionTarget]
}
```

| Field | Type | Description |
|-------|------|-------------|
| `_id` | `Guid` | Primary key, auto-generated |
| `Name` | `string` | Session display name |
| `Description` | `string` | Session description (default empty) |
| `CreatedAt` | `DateTime` | Creation timestamp (UTC) |
| `UpdatedAt` | `DateTime` | Last modification timestamp (UTC) |
| `ProjectId` | `Guid?` | FK to Project (nullable, null = unlinked) |
| `History` | `List<HistoryEntry>` | Embedded array of command history |
| `Targets` | `List<SessionTarget>` | Embedded array of targets |

---

### HistoryEntry (Embedded)

Represents a single command execution record.

```json
{
  "_id": "GUID",
  "Timestamp": "DateTime (UTC)",
  "WorkingDirectory": "string",
  "Command": "string",
  "Output": "string (max 10KB)",
  "ExitCode": "int"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `_id` | `Guid` | Unique identifier |
| `Timestamp` | `DateTime` | Execution timestamp (UTC) |
| `WorkingDirectory` | `string` | CWD when command was executed |
| `Command` | `string` | The executed command |
| `Output` | `string` | Command output (truncated to 10KB max) |
| `ExitCode` | `int` | Process exit code (0 = success) |

**Notes:**
- Output is automatically truncated to 10KB with `\n[truncated]` suffix
- History entries are appended, never modified

---

### SessionTarget (Embedded)

Represents a target machine/service in a CTF.

```json
{
  "_id": "GUID",
  "Address": "string",
  "Port": "int | null",
  "Name": "string",
  "Description": "string",
  "Type": "int (enum)"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `_id` | `Guid` | Unique identifier |
| `Address` | `string` | IP address or hostname |
| `Port` | `int?` | Port number (nullable) |
| `Name` | `string` | Display name for the target |
| `Description` | `string` | Target description (auto-filled as `"{name} {address}:{port}"` if empty) |
| `Type` | `TargetType` | Category enum (stored as int) |

---

### TargetType (Enum)

Stored as integer in database.

| Value | Name | Description |
|-------|------|-------------|
| 0 | `Unknown` | Uncategorized |
| 1 | `Web` | Web exploitation |
| 2 | `Pwn` | Binary exploitation |
| 3 | `Crypto` | Cryptography |
| 4 | `Forensics` | Forensics analysis |
| 5 | `Reverse` | Reverse engineering |
| 6 | `Misc` | Miscellaneous |

---

### CustomScript

Root document representing a reusable command template.

```json
{
  "_id": "GUID",
  "Name": "string",
  "Category": "int (enum)",
  "Template": "string"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `_id` | `Guid` | Primary key, auto-generated |
| `Name` | `string` | Script display name |
| `Category` | `ScriptCategory` | Category enum (stored as int) |
| `Template` | `string` | Command template with placeholders (e.g., `nmap -sV {host}`) |

---

### ScriptCategory (Enum)

Stored as integer in database.

| Value | Name | Description |
|-------|------|-------------|
| 0 | `Discovery` | Network discovery and scanning |
| 1 | `Web` | Web application testing |
| 2 | `ReverseShell` | Reverse shell commands |
| 3 | `Exploit` | Exploitation tools |
| 4 | `Other` | Miscellaneous |

---

## Example Document

```json
{
  "_id": { "$guid": "550e8400-e29b-41d4-a716-446655440000" },
  "Name": "HTB - Machine Challenge",
  "Description": "HackTheBox easy machine - web exploitation",
  "CreatedAt": { "$date": "2024-02-04T10:30:00Z" },
  "UpdatedAt": { "$date": "2024-02-04T11:45:00Z" },
  "History": [
    {
      "_id": { "$guid": "6ba7b810-9dad-11d1-80b4-00c04fd430c8" },
      "Timestamp": { "$date": "2024-02-04T10:32:15Z" },
      "WorkingDirectory": "/home/user",
      "Command": "nmap -sV 10.10.10.100",
      "Output": "Starting Nmap 7.94...\n22/tcp open ssh\n80/tcp open http\n...",
      "ExitCode": 0
    },
    {
      "_id": { "$guid": "6ba7b811-9dad-11d1-80b4-00c04fd430c8" },
      "Timestamp": { "$date": "2024-02-04T10:35:00Z" },
      "WorkingDirectory": "/home/user",
      "Command": "gobuster dir -u http://10.10.10.100 -w /usr/share/wordlists/common.txt",
      "Output": "/admin (Status: 200)\n/login (Status: 302)\n...\n[truncated]",
      "ExitCode": 0
    }
  ],
  "Targets": [
    {
      "_id": { "$guid": "7c9e6679-7425-40de-944b-e07fc1f90ae7" },
      "Address": "10.10.10.100",
      "Port": 80,
      "Name": "Main Web Server",
      "Description": "Main Web Server 10.10.10.100:80",
      "Type": 1
    },
    {
      "_id": { "$guid": "8d9f7780-8536-51ef-055c-f18fd2g01bf8" },
      "Address": "10.10.10.100",
      "Port": 22,
      "Name": "SSH Access",
      "Description": "SSH Access 10.10.10.100:22",
      "Type": 0
    }
  ]
}
```

### CustomScript Example

```json
{
  "_id": { "$guid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890" },
  "Name": "Full Nmap Scan",
  "Category": 0,
  "Template": "nmap -sV -sC -p- {host}"
}
```

### WriteUp

Root document representing a CTF write-up (report).

```json
{
  "_id": "GUID",
  "SessionId": "GUID",
  "FolderId": "GUID | null",
  "Name": "string",
  "Content": "string (markdown)",
  "CreatedAt": "DateTime (UTC)",
  "UpdatedAt": "DateTime (UTC)"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `_id` | `Guid` | Primary key, auto-generated |
| `SessionId` | `Guid` | Foreign key to Session (mandatory) |
| `FolderId` | `Guid?` | FK to ProjectFolder (nullable, null = unlinked) |
| `Name` | `string` | Write-up title |
| `Content` | `string` | Markdown body (default empty) |
| `CreatedAt` | `DateTime` | Creation timestamp (UTC) |
| `UpdatedAt` | `DateTime` | Last modification timestamp (UTC) |

### WriteUp Example

```json
{
  "_id": { "$guid": "d4e5f6a7-b8c9-0123-4567-89abcdef0123" },
  "SessionId": { "$guid": "550e8400-e29b-41d4-a716-446655440000" },
  "Name": "HTB Machine Writeup",
  "Content": "# Enumeration\n\n## Nmap\n\n```\nnmap -sV 10.10.10.100\n```\n\nFound ports 22 and 80 open...",
  "CreatedAt": { "$date": "2024-02-04T12:00:00Z" },
  "UpdatedAt": { "$date": "2024-02-04T14:30:00Z" }
}
```

### Media

Root document representing a binary blob (image, video, PDF, etc.).

```json
{
  "_id": "GUID",
  "FileName": "string",
  "MimeType": "string",
  "Data": "byte[] (binary)",
  "CreatedAt": "DateTime (UTC)"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `_id` | `Guid` | Primary key, auto-generated |
| `FileName` | `string` | Original file name |
| `MimeType` | `string` | MIME type (e.g., `image/png`, `application/pdf`) |
| `Data` | `byte[]` | Binary blob (max 8 MB) |
| `CreatedAt` | `DateTime` | Upload timestamp (UTC) |

### Media Example

```json
{
  "_id": { "$guid": "f1e2d3c4-b5a6-9780-1234-567890abcdef" },
  "FileName": "nmap-scan.png",
  "MimeType": "image/png",
  "Data": { "$binary": "iVBORw0KGgoAAAANSUhEUg..." },
  "CreatedAt": { "$date": "2024-02-04T12:05:00Z" }
}
```

### Project

Root document representing a CTF project. Groups sessions and write-up folders.

```json
{
  "_id": "GUID",
  "Name": "string",
  "Description": "string",
  "CreatedAt": "DateTime (UTC)",
  "UpdatedAt": "DateTime (UTC)",
  "Folders": [ProjectFolder]
}
```

| Field | Type | Description |
|-------|------|-------------|
| `_id` | `Guid` | Primary key, auto-generated |
| `Name` | `string` | Project display name |
| `Description` | `string` | Project description (default empty) |
| `CreatedAt` | `DateTime` | Creation timestamp (UTC) |
| `UpdatedAt` | `DateTime` | Last modification timestamp (UTC) |
| `Folders` | `List<ProjectFolder>` | Embedded array of folders |

---

### ProjectFolder (Embedded)

Represents a folder within a project for organizing write-ups.

```json
{
  "_id": "GUID",
  "Name": "string",
  "IsSystem": "bool"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `_id` | `Guid` | Unique identifier |
| `Name` | `string` | Folder display name |
| `IsSystem` | `bool` | `true` for auto-created folders (e.g., "Report") — cannot be deleted or renamed |

**Notes:**
- A "Report" folder with `IsSystem = true` is automatically created when a project is created.
- System folders are protected from deletion and renaming.

### Project Example

```json
{
  "_id": { "$guid": "000e1176-1ce1-4aca-8cd4-190194e6ffb2" },
  "Name": "HTB Season 2",
  "Description": "HackTheBox machines for season 2",
  "CreatedAt": { "$date": "2024-03-01T09:00:00Z" },
  "UpdatedAt": { "$date": "2024-03-01T09:00:00Z" },
  "Folders": [
    {
      "_id": { "$guid": "2f7dc82b-9de2-4ba4-b7c1-c1bb763cb50b" },
      "Name": "Report",
      "IsSystem": true
    },
    {
      "_id": { "$guid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890" },
      "Name": "Exploits",
      "IsSystem": false
    }
  ]
}
```

---

## Configuration

### BsonMapper Settings

```csharp
BsonMapper.Global.EnumAsInteger = true;  // Store enums as integers

BsonMapper.Global.Entity<Session>().Id(x => x.Id);
BsonMapper.Global.Entity<HistoryEntry>().Id(x => x.Id);
BsonMapper.Global.Entity<SessionTarget>().Id(x => x.Id);
BsonMapper.Global.Entity<CustomScript>().Id(x => x.Id);
BsonMapper.Global.Entity<WriteUp>().Id(x => x.Id);
BsonMapper.Global.Entity<Media>().Id(x => x.Id);
BsonMapper.Global.Entity<Project>().Id(x => x.Id);
BsonMapper.Global.Entity<ProjectFolder>().Id(x => x.Id);
```

### Connection Strings

| Mode | Connection String |
|------|------------------|
| Production | `Filename=ctfdeck.db` |
| In-Memory | `Filename=:memory:;Mode=Memory;Cache=Shared` |

---

## Data Flow

```
Frontend (Electron/Angular)
         │
         │ WebSocket (Binary Protocol)
         ▼
    WebSocketServer
         │
         ├── Command Execution ──► Auto-record to active session
         │                              │
         │                              ▼
         │                        SessionService
         │                              │
         ├── Session Commands ──────────┤
         │                              ▼
         │                       SessionRepository
         │                              │
         │                              ▼
         ├── Script Commands ──► CustomScriptService
         │                              │
         │                              ▼
         │                    CustomScriptRepository
         │                              │
         ├── WriteUp Commands ──► WriteUpService
         │                              │
         │                              ▼
         │                      WriteUpRepository
         │                              │
         ├── Media Commands ───► MediaService
         │                              │
         │                              ▼
         │                       MediaRepository
         │                              │
         ├── Project Commands ──► ProjectService
         │                              │
         │                              ▼
         │                     ProjectRepository
         │                              │
         └──────────────────────────────┤
                                        ▼
                                  CtfDeckDbContext
                                        │
                                        ▼
                                    LiteDB File
```

---

## Constraints & Limits

| Constraint | Value |
|------------|-------|
| Max output per command | 10 KB |
| Max media file size | 8 MB |
| Session name | No limit (string) |
| History entries per session | No limit |
| Targets per session | No limit |
| Write-ups per session | No limit |
| Media entries | No limit (global) |
| Database file size | LiteDB limit (~4GB) |

---

## File Locations

| Purpose | Path |
|---------|------|
| DbContext | `src/CtfDeck.Data/Db/CtfDeckDbContext.cs` |
| Session Persistence Model | `src/CtfDeck.Data/PersistenceModels/Sessions/` |
| Script Persistence Model | `src/CtfDeck.Data/PersistenceModels/Scripts/` |
| WriteUp Persistence Model | `src/CtfDeck.Data/PersistenceModels/WriteUps/` |
| Media Persistence Model | `src/CtfDeck.Data/PersistenceModels/Media/` |
| Session Repository | `src/CtfDeck.Data/Repositories/Sessions/SessionRepository.cs` |
| Script Repository | `src/CtfDeck.Data/Repositories/Scripts/CustomScriptRepository.cs` |
| WriteUp Repository | `src/CtfDeck.Data/Repositories/WriteUps/WriteUpRepository.cs` |
| Media Repository | `src/CtfDeck.Data/Repositories/Media/MediaRepository.cs` |
| Session Service | `src/CtfDeck.Terminal/Features/Sessions/SessionService.cs` |
| Script Service | `src/CtfDeck.Terminal/Features/Scripts/CustomScriptService.cs` |
| WriteUp Service | `src/CtfDeck.Terminal/Features/WriteUps/WriteUpService.cs` |
| Media Service | `src/CtfDeck.Terminal/Features/Media/MediaService.cs` |
| Project Persistence Model | `src/CtfDeck.Data/PersistenceModels/Projects/` |
| Project Repository | `src/CtfDeck.Data/Repositories/Projects/ProjectRepository.cs` |
| Project Service | `src/CtfDeck.Terminal/Features/Projects/ProjectService.cs` |
