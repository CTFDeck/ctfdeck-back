# Database Schema - Session Management

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

---

## Document Schemas

### Session

Root document representing a CTF session.

```json
{
  "_id": "GUID",
  "Name": "string",
  "CreatedAt": "DateTime (UTC)",
  "UpdatedAt": "DateTime (UTC)",
  "History": [HistoryEntry],
  "Targets": [SessionTarget]
}
```

| Field | Type | Description |
|-------|------|-------------|
| `_id` | `Guid` | Primary key, auto-generated |
| `Name` | `string` | Session display name |
| `CreatedAt` | `DateTime` | Creation timestamp (UTC) |
| `UpdatedAt` | `DateTime` | Last modification timestamp (UTC) |
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
  "Type": "int (enum)"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `_id` | `Guid` | Unique identifier |
| `Address` | `string` | IP address or hostname |
| `Port` | `int?` | Port number (nullable) |
| `Name` | `string` | Display name for the target |
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

## Example Document

```json
{
  "_id": { "$guid": "550e8400-e29b-41d4-a716-446655440000" },
  "Name": "HTB - Machine Challenge",
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
      "Type": 1
    },
    {
      "_id": { "$guid": "8d9f7780-8536-51ef-055c-f18fd2g01bf8" },
      "Address": "10.10.10.100",
      "Port": 22,
      "Name": "SSH Access",
      "Type": 0
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
```

### Connection Strings

| Mode | Connection String |
|------|------------------|
| Production | `Filename=ctfdeck_sessions.db` |
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
         └── Session Commands ──────────┤
                                        ▼
                                 SessionRepository
                                        │
                                        ▼
                                 SessionDbContext
                                        │
                                        ▼
                                    LiteDB File
```

---

## Constraints & Limits

| Constraint | Value |
|------------|-------|
| Max output per command | 10 KB |
| Session name | No limit (string) |
| History entries per session | No limit |
| Targets per session | No limit |
| Database file size | LiteDB limit (~4GB) |

---

## File Locations

| Purpose | Path |
|---------|------|
| Models | `src/CtfDeck.Terminal/Session/Models/` |
| DbContext | `src/CtfDeck.Terminal/Session/Data/SessionDbContext.cs` |
| Repository | `src/CtfDeck.Terminal/Session/Repositories/SessionRepository.cs` |
| Service | `src/CtfDeck.Terminal/Session/Services/SessionService.cs` |
