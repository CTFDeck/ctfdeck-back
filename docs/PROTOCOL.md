# CtfDeck WebSocket Binary Protocol

## Overview

The CtfDeck WebSocket Binary Protocol is a high-performance binary protocol designed for real-time command execution and response handling over WebSocket connections. It provides efficient serialization, message correlation, and error handling for terminal command execution.

**Key Features:**
- Binary message format for optimal performance
- Message correlation using UUIDs
- UTF-8 encoded command strings
- Multi-client support
- Cross-platform compatibility

## Connection Handshake

The protocol operates over standard WebSocket connections. Clients must perform a WebSocket upgrade handshake:

```http
GET / HTTP/1.1
Host: localhost:42712
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==
Sec-WebSocket-Protocol:
Sec-WebSocket-Version: 13
```

## Message Format

All messages are transmitted as binary WebSocket frames using little-endian byte ordering.

All messages start with a 1-byte **type prefix** that identifies the message kind. This applies to every message in the protocol (commands, responses, session operations, etc.).

### Command Message (type = 6)

Clients send commands using the following binary structure:

```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (6 = CommandExecute)
1      | 4    | int32     | Command length (N)
5      | N    | bytes[]   | Command string (UTF-8)
5+N    | 16   | bytes[16] | Message ID (UUID)
```

**Total Size:** 1 + 4 + N + 16 bytes

### Response Message (type = 0)

Servers respond with the following binary structure:

```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (0 = CompleteResponse)
1      | 4    | int32     | Exit code
5      | 4    | int32     | Output length (M)
9      | M    | bytes[]   | Output string (UTF-8)
9+M    | 4    | int32     | Error length (K)
9+M+4  | K    | bytes[]   | Error string (UTF-8)
9+M+K+4| 4   | int32     | Working directory length (W)
9+M+K+8| W   | bytes[]   | Working directory (UTF-8)
9+M+K+W+8|16 | bytes[16] | Message ID (UUID)
```

## Message Types

All message types (1-byte prefix):

| Type | Value | Direction | Description |
|------|-------|-----------|-------------|
| CompleteResponse | 0 | Server → Client | Complete command result (cd) |
| StreamOutput | 1 | Server → Client | Streaming stdout chunk |
| StreamError | 2 | Server → Client | Streaming stderr chunk |
| StreamEnd | 3 | Server → Client | Stream finished (exitCode + cwd) |
| CommandKill | 4 | Client → Server | Cancel a running command |
| CommandKillResult | 5 | Server → Client | Kill result |
| CommandExecute | 6 | Client → Server | Execute a terminal command |
| SessionCreate | 10 | Client → Server | Create a new session |
| SessionSetActive | 11 | Client → Server | Set active session for recording |
| SessionLoad | 12 | Client → Server | Load full session data |
| SessionList | 13 | Client → Server | List all sessions (metadata) |
| SessionDelete | 14 | Client → Server | Delete a session |
| SessionUpdateTargets | 15 | Client → Server | Bulk sync targets to session |
| SessionUpdate | 16 | Client → Server | Update session name and description |
| SessionAddTarget | 17 | Client → Server | Add a single target to session |
| SessionDeleteTarget | 18 | Client → Server | Delete a single target from session |
| SessionEditTarget | 19 | Client → Server | Edit a single target in session |
| SessionCreateResult | 20 | Server → Client | Response to SessionCreate |
| SessionSetActiveResult | 21 | Server → Client | Response to SessionSetActive |
| SessionLoadResult | 22 | Server → Client | Response to SessionLoad |
| SessionListResult | 23 | Server → Client | Response to SessionList |
| SessionDeleteResult | 24 | Server → Client | Response to SessionDelete |
| SessionUpdateResult | 25 | Server → Client | Response to SessionUpdate |
| SessionAddTargetResult | 26 | Server → Client | Response to SessionAddTarget |
| SessionDeleteTargetResult | 27 | Server → Client | Response to SessionDeleteTarget |
| SessionEditTargetResult | 28 | Server → Client | Response to SessionEditTarget |
| SessionOperationError | 29 | Server → Client | Session error response |
| CustomScriptCreate | 30 | Client → Server | Create a custom script |
| CustomScriptUpdate | 31 | Client → Server | Update a custom script |
| CustomScriptDelete | 32 | Client → Server | Delete a custom script |
| CustomScriptList | 33 | Client → Server | List all custom scripts |
| CustomScriptCreateResult | 40 | Server → Client | Response to CustomScriptCreate |
| CustomScriptUpdateResult | 41 | Server → Client | Response to CustomScriptUpdate |
| CustomScriptDeleteResult | 42 | Server → Client | Response to CustomScriptDelete |
| CustomScriptListResult | 43 | Server → Client | Response to CustomScriptList |
| CustomScriptOperationError | 49 | Server → Client | Custom script error response |
| WriteUpCreate | 50 | Client → Server | Create a new write-up |
| WriteUpUpdate | 51 | Client → Server | Update a write-up |
| WriteUpDelete | 52 | Client → Server | Delete a write-up |
| WriteUpList | 53 | Client → Server | List write-ups for a session |
| WriteUpLoad | 54 | Client → Server | Load full write-up data |
| WriteUpMove | 55 | Client → Server | Move write-up to a folder |
| WriteUpCreateResult | 60 | Server → Client | Response to WriteUpCreate |
| WriteUpUpdateResult | 61 | Server → Client | Response to WriteUpUpdate |
| WriteUpDeleteResult | 62 | Server → Client | Response to WriteUpDelete |
| WriteUpListResult | 63 | Server → Client | Response to WriteUpList |
| WriteUpLoadResult | 64 | Server → Client | Response to WriteUpLoad |
| WriteUpMoveResult | 65 | Server → Client | Response to WriteUpMove |
| WriteUpOperationError | 69 | Server → Client | Write-up error response |
| MediaUpload | 70 | Client → Server | Upload a media file |
| MediaLoad | 71 | Client → Server | Load a media entry (with blob) |
| MediaDelete | 72 | Client → Server | Delete a media entry |
| MediaList | 73 | Client → Server | List all media (metadata only) |
| MediaUploadResult | 80 | Server → Client | Response to MediaUpload |
| MediaLoadResult | 81 | Server → Client | Response to MediaLoad |
| MediaDeleteResult | 82 | Server → Client | Response to MediaDelete |
| MediaListResult | 83 | Server → Client | Response to MediaList |
| MediaOperationError | 89 | Server → Client | Media error response |
| ProjectCreate | 90 | Client → Server | Create a new project |
| ProjectLoad | 91 | Client → Server | Load full project data |
| ProjectList | 92 | Client → Server | List all projects (metadata) |
| ProjectUpdate | 93 | Client → Server | Update project |
| ProjectDelete | 94 | Client → Server | Delete a project |
| ProjectAddFolder | 95 | Client → Server | Add folder to project |
| ProjectDeleteFolder | 96 | Client → Server | Delete folder from project |
| ProjectRenameFolder | 97 | Client → Server | Rename folder |
| ProjectAssignSession | 98 | Client → Server | Assign session to project |
| ProjectCreateResult | 100 | Server → Client | Response to ProjectCreate |
| ProjectLoadResult | 101 | Server → Client | Response to ProjectLoad |
| ProjectListResult | 102 | Server → Client | Response to ProjectList |
| ProjectUpdateResult | 103 | Server → Client | Response to ProjectUpdate |
| ProjectDeleteResult | 104 | Server → Client | Response to ProjectDelete |
| ProjectAddFolderResult | 105 | Server → Client | Response to ProjectAddFolder |
| ProjectDeleteFolderResult | 106 | Server → Client | Response to ProjectDeleteFolder |
| ProjectRenameFolderResult | 107 | Server → Client | Response to ProjectRenameFolder |
| ProjectAssignSessionResult | 108 | Server → Client | Response to ProjectAssignSession |
| ProjectOperationError | 109 | Server → Client | Project error response |
| ProjectListSessions | 110 | Client → Server | List sessions for a project |
| ProjectListSessionsResult | 111 | Server → Client | Response to ProjectListSessions |
| ProjectListWriteUps | 112 | Client → Server | List write-ups for a folder |
| ProjectListWriteUpsResult | 113 | Server → Client | Response to ProjectListWriteUps |
| ProjectExport | 114 | Client → Server | Export project to JSON file |
| ProjectExportResult | 115 | Server → Client | Response to ProjectExport |
| ProjectImport | 116 | Client → Server | Import project from JSON file |
| ProjectImportResult | 117 | Server → Client | Response to ProjectImport |

## Error Codes

| Code | Description |
|------|-------------|
| 0    | Success - command executed successfully |
| 1    | Command failed - non-zero exit code |
| -1   | Protocol error - malformed message |
| 2    | Invalid command format |
| 127  | Command not found |

## Message Flow

```
Client                      Server
  |                           |
  |-- WebSocket Upgrade -->    |
  |                           |
  |-- Binary Command --------> |
  |   (cmd_length, cmd, id)   |
  |                           |
  |<-- Binary Response -------|
  |   (exit_code, out, err, id)|
  |                           |
  |-- Close Connection -----> |
```

## Implementation Example

### C# (.NET)

```csharp
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Buffers.Binary;

public class CtfDeckClient : IDisposable
{
    private readonly ClientWebSocket _webSocket;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<CommandResponse>> _pendingCommands;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public CtfDeckClient(string uri)
    {
        _webSocket = new ClientWebSocket();
        _pendingCommands = new ConcurrentDictionary<string, TaskCompletionSource<CommandResponse>>();
        _cancellationTokenSource = new CancellationTokenSource();

        _ = Task.Run(MessageLoop, _cancellationTokenSource.Token);
    }

    public async Task ConnectAsync(string uri)
    {
        await _webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
    }

    public byte[] SerializeCommand(string command, Guid messageId)
    {
        var commandBytes = Encoding.UTF8.GetBytes(command);
        var messageIdBytes = messageId.ToByteArray();

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)6); // CommandExecute
        writer.Write(commandBytes.Length);
        writer.Write(commandBytes);
        writer.Write(messageIdBytes);

        return stream.ToArray();
    }

    public CommandResponse DeserializeResponse(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        var exitCode = reader.ReadInt32();
        var outputLength = reader.ReadInt32();
        var outputBytes = reader.ReadBytes(outputLength);
        var errorLength = reader.ReadInt32();
        var errorBytes = reader.ReadBytes(errorLength);
        var messageIdBytes = reader.ReadBytes(16);

        return new CommandResponse
        {
            ExitCode = exitCode,
            Output = Encoding.UTF8.GetString(outputBytes),
            Error = Encoding.UTF8.GetString(errorBytes),
            MessageId = new Guid(messageIdBytes)
        };
    }

    public async Task<CommandResponse> ExecuteCommandAsync(string command)
    {
        var messageId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<CommandResponse>();
        _pendingCommands[messageId.ToString()] = tcs;

        var message = SerializeCommand(command, messageId);
        await _webSocket.SendAsync(new ArraySegment<byte>(message),
            WebSocketMessageType.Binary, true, CancellationToken.None);

        return await tcs.Task;
    }

    private async Task MessageLoop()
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var response = DeserializeResponse(buffer[..result.Count]);

                    if (_pendingCommands.TryRemove(response.MessageId.ToString(), out var tcs))
                    {
                        tcs.SetResult(response);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when shutting down
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _webSocket?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}

public class CommandResponse
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
    public Guid MessageId { get; set; }
}

// Usage example
public class Program
{
    public static async Task Main(string[] args)
    {
        using var client = new CtfDeckClient("ws://localhost:42712");
        await client.ConnectAsync("ws://localhost:42712");

        try
        {
            var response = await client.ExecuteCommandAsync("ls -la");
            Console.WriteLine($"Exit Code: {response.ExitCode}");
            Console.WriteLine($"Output: {response.Output}");
            if (!string.IsNullOrEmpty(response.Error))
            {
                Console.WriteLine($"Error: {response.Error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Command failed: {ex.Message}");
        }
    }
}
```

## Security Considerations

### Input Validation
- Commands should be validated on the server side
- Limit command length to prevent buffer overflow attacks
- Sanitize input to prevent injection attacks

### Connection Security
- Implement authentication/authorization as needed
- Consider rate limiting to prevent abuse
- No need of websocket secure connection because its intended to be executed locally only.

### Message Size Limits
- Maximum command length: 1MB
- Maximum output length: 10MB
- Maximum error length: 1MB

## Implementation Guidelines

### Error Handling
- Always validate message format before processing
- Return appropriate error codes for different failure scenarios
- Log protocol errors for debugging

### Performance
- Reuse buffers for serialization/deserialization
- Implement connection pooling for high-load scenarios
- Consider compression for large outputs

### Compatibility
- Protocol version 1.0
- Backward compatibility considerations for future versions
- Test with different WebSocket implementations

## Debugging

### WebSocket Tools
- Chrome DevTools Network panel
- WebSocket King browser extension
- `wscat` command-line tool

### Message Inspection
```bash
# Using wscat for debugging
npm install -g wscat
wscat -c "ws://localhost:42712"
```

### Common Issues
- Check byte order (little-endian required)
- Verify UTF-8 encoding
- Ensure proper message boundaries
- Validate UUID format

---

## Parallel Command Execution

The server supports executing multiple commands concurrently. When a client sends a new command while another is still running, both execute in parallel with independent streaming outputs. Each command is identified by its unique `messageId`.

**Behavior:**
- Streaming commands (ls, cat, nmap, etc.) are dispatched in background tasks — fire-and-forget
- `cd` commands remain sequential (they modify shared working directory state)
- Session and CommandKill messages are processed inline in the message loop
- `WebSocket.SendAsync` calls are serialized per-client via a `SemaphoreSlim` (not thread-safe natively)

### CommandKill

Allows the client to cancel a running command by its `messageId`.

#### CommandKill (client → server)
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (4)
1      | 16   | bytes[16] | Command ID (UUID of the command to kill)
```

**Total Size:** 17 bytes

#### CommandKillResult (server → client)
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (5)
1      | 16   | bytes[16] | Command ID (UUID)
17     | 1    | byte      | Success (1 = killed, 0 = not found)
```

**Total Size:** 18 bytes

**Notes:**
- When a command is killed, it sends a `StreamEnd` message with `exitCode = -1` before the `CommandKillResult`
- If the command has already finished, `success` will be `0`
- On client disconnect, all active commands for that client are automatically cancelled

---

## Session Management Protocol

### Overview

The Session Management Protocol extends the base terminal protocol to support persistent sessions. Sessions store command history and targets, enabling users to save and restore their CTF work.

**Key Features:**
- Automatic command history recording
- Target management (IP, ports, challenge types)
- Output truncation (10KB max per command)
- LiteDB persistence on server side

### Message Types

All session messages use a 1-byte type prefix to differentiate from terminal messages.

| Type | Value | Direction | Description |
|------|-------|-----------|-------------|
| SessionCreate | 10 | Client → Server | Create a new session |
| SessionSetActive | 11 | Client → Server | Set active session for recording |
| SessionLoad | 12 | Client → Server | Load full session data |
| SessionList | 13 | Client → Server | List all sessions (metadata) |
| SessionDelete | 14 | Client → Server | Delete a session |
| SessionUpdateTargets | 15 | Client → Server | Bulk sync targets to session |
| SessionUpdate | 16 | Client → Server | Update session name and description |
| SessionAddTarget | 17 | Client → Server | Add a single target to session |
| SessionDeleteTarget | 18 | Client → Server | Delete a single target from session |
| SessionEditTarget | 19 | Client → Server | Edit a single target in session |
| SessionCreateResult | 20 | Server → Client | Response to SessionCreate |
| SessionSetActiveResult | 21 | Server → Client | Response to SessionSetActive |
| SessionLoadResult | 22 | Server → Client | Response to SessionLoad |
| SessionListResult | 23 | Server → Client | Response to SessionList |
| SessionDeleteResult | 24 | Server → Client | Response to SessionDelete |
| SessionUpdateResult | 25 | Server → Client | Response to SessionUpdate |
| SessionAddTargetResult | 26 | Server → Client | Response to SessionAddTarget |
| SessionDeleteTargetResult | 27 | Server → Client | Response to SessionDeleteTarget |
| SessionEditTargetResult | 28 | Server → Client | Response to SessionEditTarget |
| SessionOperationError | 29 | Server → Client | Error response |

### Request Message Formats

#### SessionCreate
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (10)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 4    | int32     | Name length (N)
21     | N    | bytes[]   | Session name (UTF-8)
```

#### SessionSetActive
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (11)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Session ID (UUID, empty GUID to clear)
```

#### SessionLoad
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (12)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Session ID (UUID)
```

#### SessionList
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (13)
1      | 16   | bytes[16] | Message ID (UUID)
```

#### SessionDelete
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (14)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Session ID (UUID)
```

#### SessionUpdateTargets
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (15)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Session ID (UUID)
33     | 4    | int32     | Target count (N)
37     | ...  | Target[]  | Array of targets
```

**Target structure:**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Target ID (UUID)
16     | 4    | int32     | Address length (A)
20     | A    | bytes[]   | Address (UTF-8)
20+A   | 4    | int32     | Port (-1 if null)
24+A   | 4    | int32     | Name length (N)
28+A   | N    | bytes[]   | Name (UTF-8)
28+A+N | 4    | int32     | Description length (D)
32+A+N | D    | bytes[]   | Description (UTF-8)
32+A+N+D| 4   | int32     | Target type (enum)
```

#### SessionUpdate
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (16)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Session ID (UUID)
33     | 4    | int32     | Name length (N)
37     | N    | bytes[]   | Session name (UTF-8)
37+N   | 4    | int32     | Description length (D)
41+N   | D    | bytes[]   | Session description (UTF-8)
```

#### SessionAddTarget
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (17)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Session ID (UUID)
33     | 4    | int32     | Address length (A)
37     | A    | bytes[]   | Address (UTF-8)
37+A   | 4    | int32     | Port (-1 if null)
41+A   | 4    | int32     | Name length (N)
45+A   | N    | bytes[]   | Name (UTF-8)
45+A+N | 4    | int32     | Description length (D)
49+A+N | D    | bytes[]   | Description (UTF-8)
49+A+N+D| 4   | int32     | Target type (enum)
```

#### SessionDeleteTarget
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (18)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Session ID (UUID)
33     | 16   | bytes[16] | Target ID (UUID)
```

#### SessionEditTarget
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (19)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Session ID (UUID)
33     | 16   | bytes[16] | Target ID (UUID)
49     | 4    | int32     | Address length (A)
53     | A    | bytes[]   | Address (UTF-8)
53+A   | 4    | int32     | Port (-1 if null)
57+A   | 4    | int32     | Name length (N)
61+A   | N    | bytes[]   | Name (UTF-8)
61+A+N | 4    | int32     | Description length (D)
65+A+N | D    | bytes[]   | Description (UTF-8)
65+A+N+D| 4   | int32     | Target type (enum)
```

### Response Message Formats

#### SessionCreateResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (20)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | 16   | bytes[16] | Created session ID (UUID)
```

#### SessionSetActiveResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (21)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### SessionLoadResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (22)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | ...  | Session   | Session data (if success)
```

**Session structure:**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Session ID (UUID)
16     | 4    | int32     | Name length (N)
20     | N    | bytes[]   | Name (UTF-8)
20+N   | 4    | int32     | Description length (D)
24+N   | D    | bytes[]   | Description (UTF-8)
24+N+D | 8    | int64     | CreatedAt (.NET ticks)
32+N+D | 8    | int64     | UpdatedAt (.NET ticks)
40+N+D | 16   | bytes[16] | Project ID (UUID, Guid.Empty = no project)
56+N+D | 4    | int32     | History count (H)
60+N+D | ...  | Entry[]   | History entries
...    | 4    | int32     | Target count (T)
...    | ...  | Target[]  | Targets
```

**HistoryEntry structure:**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Entry ID (UUID)
16     | 8    | int64     | Timestamp (.NET ticks)
24     | 4    | int32     | WorkingDirectory length (W)
28     | W    | bytes[]   | WorkingDirectory (UTF-8)
28+W   | 4    | int32     | Command length (C)
32+W   | C    | bytes[]   | Command (UTF-8)
32+W+C | 4    | int32     | Output length (O)
36+W+C | O    | bytes[]   | Output (UTF-8, max 10KB)
36+W+C+O| 4   | int32     | Exit code
```

#### SessionListResult
```
OFFSET | SIZE | TYPE        | DESCRIPTION
0      | 1    | byte        | Message type (23)
1      | 16   | bytes[16]   | Message ID (UUID)
17     | 4    | int32       | Session count (N)
21     | ...  | Metadata[]  | Session metadata array
```

**SessionMetadata structure:**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Session ID (UUID)
16     | 4    | int32     | Name length (N)
20     | N    | bytes[]   | Name (UTF-8)
20+N   | 4    | int32     | Description length (D)
24+N   | D    | bytes[]   | Description (UTF-8)
24+N+D | 8    | int64     | CreatedAt (.NET ticks)
32+N+D | 8    | int64     | UpdatedAt (.NET ticks)
40+N+D | 4    | int32     | History count
44+N+D | 4    | int32     | Target count
48+N+D | 16   | bytes[16] | Project ID (UUID, Guid.Empty = no project)
```

#### SessionDeleteResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (24)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### SessionUpdateResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (25)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### SessionAddTargetResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (26)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | 16   | bytes[16] | Created target ID (UUID)
```

#### SessionDeleteTargetResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (27)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### SessionEditTargetResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (28)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### SessionOperationError
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (29)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 4    | int32     | Error length (E)
21     | E    | bytes[]   | Error message (UTF-8)
```

### Target Types

| Value | Name | Description |
|-------|------|-------------|
| 0 | Unknown | Unspecified challenge type |
| 1 | Web | Web exploitation |
| 2 | Pwn | Binary exploitation |
| 3 | Crypto | Cryptography |
| 4 | Forensics | Digital forensics |
| 5 | Reverse | Reverse engineering |
| 6 | Misc | Miscellaneous |

### Automatic Command Recording

When a session is active, the server automatically records every command executed:

**Recording behavior:**
- Commands are recorded with timestamp, working directory, and exit code
- Output is truncated to 10KB maximum (with `\n[truncated]` suffix)
- Both streaming and non-streaming commands are recorded
- Recording happens after command completion

### Output Truncation

To prevent database bloat, command outputs are limited:

```
MAX_OUTPUT_SIZE = 10 * 1024  // 10KB
TRUNCATED_SUFFIX = "\n[truncated]"

if output.byteLength > MAX_OUTPUT_SIZE:
    output = output[0:MAX_OUTPUT_SIZE - TRUNCATED_SUFFIX.length] + TRUNCATED_SUFFIX
```

### DateTime Encoding

Timestamps use .NET ticks (100-nanosecond intervals since 0001-01-01):

```javascript
// JavaScript: Convert ticks to Date
function ticksToDate(ticks) {
    const epochDiff = BigInt('621355968000000000');
    const ticksPerMs = BigInt(10000);
    const ms = Number((ticks - epochDiff) / ticksPerMs);
    return new Date(ms);
}

// JavaScript: Convert Date to ticks
function dateToTicks(date) {
    const epochDiff = BigInt('621355968000000000');
    const ticksPerMs = BigInt(10000);
    return epochDiff + BigInt(date.getTime()) * ticksPerMs;
}
```

### Storage

Sessions are persisted using LiteDB:
- **File location:** `ctfdeck_sessions.db` (same directory as executable)
- **Collections:** `sessions`
- **Indexes:** `Id` (unique), `Name`

### Error Handling

| Error | Cause | Resolution |
|-------|-------|------------|
| Session not found | Invalid session ID | Use SessionList to get valid IDs |
| SetActive failed | Session doesn't exist | Create session first |
| Database locked | Concurrent access | Retry operation |

---

## Custom Scripts Protocol

### Overview

The Custom Scripts Protocol extends the base protocol to support reusable command templates. Scripts are stored globally (not tied to a session) and contain a name, category, and command template string with placeholder variables (e.g., `{host}`, `{port}`).

**Key Features:**
- CRUD operations for custom scripts
- Category-based organization
- Template variables for dynamic command generation
- LiteDB persistence on server side

### Message Types

| Type | Value | Direction | Description |
|------|-------|-----------|-------------|
| CustomScriptCreate | 30 | Client → Server | Create a new script |
| CustomScriptUpdate | 31 | Client → Server | Update an existing script |
| CustomScriptDelete | 32 | Client → Server | Delete a script |
| CustomScriptList | 33 | Client → Server | List all scripts |
| CustomScriptCreateResult | 40 | Server → Client | Response to CustomScriptCreate |
| CustomScriptUpdateResult | 41 | Server → Client | Response to CustomScriptUpdate |
| CustomScriptDeleteResult | 42 | Server → Client | Response to CustomScriptDelete |
| CustomScriptListResult | 43 | Server → Client | Response to CustomScriptList |
| CustomScriptOperationError | 49 | Server → Client | Error response |

### Request Message Formats

#### CustomScriptCreate
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (30)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 4    | int32     | Name length (N)
21     | N    | bytes[]   | Script name (UTF-8)
21+N   | 4    | int32     | Category (enum as int)
25+N   | 4    | int32     | Template length (T)
29+N   | T    | bytes[]   | Template string (UTF-8)
```

#### CustomScriptUpdate
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (31)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Script ID (UUID)
33     | 4    | int32     | Name length (N)
37     | N    | bytes[]   | Script name (UTF-8)
37+N   | 4    | int32     | Category (enum as int)
41+N   | 4    | int32     | Template length (T)
45+N   | T    | bytes[]   | Template string (UTF-8)
```

#### CustomScriptDelete
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (32)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Script ID (UUID)
```

#### CustomScriptList
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (33)
1      | 16   | bytes[16] | Message ID (UUID)
```

### Response Message Formats

#### CustomScriptCreateResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (40)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | 16   | bytes[16] | Created script ID (UUID)
```

#### CustomScriptUpdateResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (41)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### CustomScriptDeleteResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (42)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### CustomScriptListResult
```
OFFSET | SIZE | TYPE        | DESCRIPTION
0      | 1    | byte        | Message type (43)
1      | 16   | bytes[16]   | Message ID (UUID)
17     | 4    | int32       | Script count (N)
21     | ...  | Script[]    | Array of scripts
```

**CustomScript structure:**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Script ID (UUID)
16     | 4    | int32     | Name length (N)
20     | N    | bytes[]   | Name (UTF-8)
20+N   | 4    | int32     | Category (enum as int)
24+N   | 4    | int32     | Template length (T)
28+N   | T    | bytes[]   | Template (UTF-8)
```

#### CustomScriptOperationError
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (49)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 4    | int32     | Error length (E)
21     | E    | bytes[]   | Error message (UTF-8)
```

### Script Categories

| Value | Name | Description |
|-------|------|-------------|
| 0 | Discovery | Network discovery and scanning |
| 1 | Web | Web application testing |
| 2 | ReverseShell | Reverse shell commands |
| 3 | Exploit | Exploitation tools |
| 4 | Other | Miscellaneous |

### Storage

Custom scripts are persisted using LiteDB:
- **Collection:** `customscripts`
- **Indexes:** `Id` (unique)

### Error Handling

| Error | Cause | Resolution |
|-------|-------|------------|
| Script not found | Invalid script ID on update/delete | Use CustomScriptList to get valid IDs |
| Database locked | Concurrent access | Retry operation |

---

## Write-Up Protocol

### Overview

The Write-Up Protocol extends the base protocol to support CTF write-ups (reports). Write-ups are linked to a session (mandatory `SessionId`), contain a name and markdown content, and support full CRUD operations. Write-ups are stored as separate documents (not embedded in sessions) to allow independent management.

**Key Features:**
- CRUD operations for write-ups
- Session-scoped listing (by `SessionId`)
- Markdown content storage
- Separate metadata and full-content load paths
- LiteDB persistence on server side

### Message Types

| Type | Value | Direction | Description |
|------|-------|-----------|-------------|
| WriteUpCreate | 50 | Client → Server | Create a new write-up |
| WriteUpUpdate | 51 | Client → Server | Update name and content |
| WriteUpDelete | 52 | Client → Server | Delete a write-up |
| WriteUpList | 53 | Client → Server | List write-ups for a session (metadata) |
| WriteUpLoad | 54 | Client → Server | Load full write-up data |
| WriteUpCreateResult | 60 | Server → Client | Response to WriteUpCreate |
| WriteUpUpdateResult | 61 | Server → Client | Response to WriteUpUpdate |
| WriteUpDeleteResult | 62 | Server → Client | Response to WriteUpDelete |
| WriteUpListResult | 63 | Server → Client | Response to WriteUpList |
| WriteUpLoadResult | 64 | Server → Client | Response to WriteUpLoad |
| WriteUpMove | 55 | Client → Server | Move write-up to a folder |
| WriteUpMoveResult | 65 | Server → Client | Response to WriteUpMove |
| WriteUpOperationError | 69 | Server → Client | Error response |

### Request Message Formats

#### WriteUpCreate
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (50)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Session ID (UUID)
33     | 4    | int32     | Name length (N)
37     | N    | bytes[]   | Write-up name (UTF-8)
```

#### WriteUpUpdate
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (51)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Write-up ID (UUID)
33     | 4    | int32     | Name length (N)
37     | N    | bytes[]   | Write-up name (UTF-8)
37+N   | 4    | int32     | Content length (C)
41+N   | C    | bytes[]   | Markdown content (UTF-8)
```

#### WriteUpDelete
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (52)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Write-up ID (UUID)
```

#### WriteUpList
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (53)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Session ID (UUID)
```

#### WriteUpLoad
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (54)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Write-up ID (UUID)
```

#### WriteUpMove
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (55)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Write-up ID (UUID)
33     | 16   | bytes[16] | Folder ID (UUID, Guid.Empty to unlink)
```

**Note:** This message is routed through the Project message handler, not the WriteUp handler.

### Response Message Formats

#### WriteUpCreateResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (60)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | 16   | bytes[16] | Created write-up ID (UUID)
```

#### WriteUpUpdateResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (61)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### WriteUpDeleteResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (62)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### WriteUpListResult
```
OFFSET | SIZE | TYPE        | DESCRIPTION
0      | 1    | byte        | Message type (63)
1      | 16   | bytes[16]   | Message ID (UUID)
17     | 4    | int32       | Write-up count (N)
21     | ...  | Metadata[]  | Write-up metadata array
```

**WriteUpMetadata structure:**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Write-up ID (UUID)
16     | 16   | bytes[16] | Session ID (UUID)
32     | 16   | bytes[16] | Folder ID (UUID, Guid.Empty = no folder)
48     | 4    | int32     | Name length (N)
52     | N    | bytes[]   | Name (UTF-8)
52+N   | 8    | int64     | CreatedAt (.NET ticks)
60+N   | 8    | int64     | UpdatedAt (.NET ticks)
```

#### WriteUpLoadResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (64)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | ...  | WriteUp   | Full write-up data (if success)
```

**WriteUp structure (when success = 1):**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Write-up ID (UUID)
16     | 16   | bytes[16] | Session ID (UUID)
32     | 16   | bytes[16] | Folder ID (UUID, Guid.Empty = no folder)
48     | 4    | int32     | Name length (N)
52     | N    | bytes[]   | Name (UTF-8)
52+N   | 4    | int32     | Content length (C)
56+N   | C    | bytes[]   | Markdown content (UTF-8)
56+N+C | 8    | int64     | CreatedAt (.NET ticks)
64+N+C | 8    | int64     | UpdatedAt (.NET ticks)
```

#### WriteUpMoveResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (65)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### WriteUpOperationError
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (69)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 4    | int32     | Error length (E)
21     | E    | bytes[]   | Error message (UTF-8)
```

### Storage

Write-ups are persisted using LiteDB:
- **Collection:** `writeups`
- **Indexes:** `Id` (unique), `SessionId`

### Error Handling

| Error | Cause | Resolution |
|-------|-------|------------|
| Write-up not found | Invalid write-up ID on load/update/delete | Use WriteUpList to get valid IDs |
| Session not found | Invalid session ID on create/list | Use SessionList to get valid session IDs |
| Database locked | Concurrent access | Retry operation |

---

## Media Protocol

### Overview

The Media Protocol extends the base protocol to support binary blob storage (images, videos, PDFs, etc.). Media entries are stored globally (not tied to a session) and can be referenced from write-up markdown content by their ID. The server enforces an **8 MB per-file upload limit**.

**Key Features:**
- Upload, load, delete, and list operations
- Binary blob storage with filename and MIME type metadata
- 8 MB per-file upload limit (server-enforced)
- Separate metadata listing (no blob) and full load (with blob) paths
- Multi-frame WebSocket message support for large uploads
- LiteDB persistence on server side

### Message Types

| Type | Value | Direction | Description |
|------|-------|-----------|-------------|
| MediaUpload | 70 | Client → Server | Upload a media file |
| MediaLoad | 71 | Client → Server | Load a media entry (with blob) |
| MediaDelete | 72 | Client → Server | Delete a media entry |
| MediaList | 73 | Client → Server | List all media (metadata only) |
| MediaUploadResult | 80 | Server → Client | Response to MediaUpload |
| MediaLoadResult | 81 | Server → Client | Response to MediaLoad |
| MediaDeleteResult | 82 | Server → Client | Response to MediaDelete |
| MediaListResult | 83 | Server → Client | Response to MediaList |
| MediaOperationError | 89 | Server → Client | Error response |

### Request Message Formats

#### MediaUpload
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (70)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 4    | int32     | File name length (F)
21     | F    | bytes[]   | File name (UTF-8)
21+F   | 4    | int32     | MIME type length (M)
25+F   | M    | bytes[]   | MIME type (UTF-8)
25+F+M | 4    | int32     | Data length (D)
29+F+M | D    | bytes[]   | Binary data
```

**Notes:**
- Maximum data length is 8 MB (8,388,608 bytes). The server rejects uploads exceeding this limit.
- Large uploads may span multiple WebSocket frames. The server accumulates frames until `EndOfMessage` before processing.

#### MediaLoad
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (71)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Media ID (UUID)
```

#### MediaDelete
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (72)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Media ID (UUID)
```

#### MediaList
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (73)
1      | 16   | bytes[16] | Message ID (UUID)
```

### Response Message Formats

#### MediaUploadResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (80)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | 16   | bytes[16] | Created media ID (UUID)
```

#### MediaLoadResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (81)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | ...  | Media     | Full media data (if success)
```

**Media structure (when success = 1):**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Media ID (UUID)
16     | 4    | int32     | File name length (F)
20     | F    | bytes[]   | File name (UTF-8)
20+F   | 4    | int32     | MIME type length (M)
24+F   | M    | bytes[]   | MIME type (UTF-8)
24+F+M | 4    | int32     | Data length (D)
28+F+M | D    | bytes[]   | Binary data
```

#### MediaDeleteResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (82)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### MediaListResult
```
OFFSET | SIZE | TYPE        | DESCRIPTION
0      | 1    | byte        | Message type (83)
1      | 16   | bytes[16]   | Message ID (UUID)
17     | 4    | int32       | Media count (N)
21     | ...  | Metadata[]  | Media metadata array
```

**MediaMetadata structure:**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Media ID (UUID)
16     | 4    | int32     | File name length (F)
20     | F    | bytes[]   | File name (UTF-8)
20+F   | 4    | int32     | MIME type length (M)
24+F   | M    | bytes[]   | MIME type (UTF-8)
24+F+M | 8    | int64     | CreatedAt (.NET ticks)
```

#### MediaOperationError
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (89)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 4    | int32     | Error length (E)
21     | E    | bytes[]   | Error message (UTF-8)
```

### Multi-Frame WebSocket Messages

Media uploads can exceed the WebSocket receive buffer (64 KB). The server handles this transparently:

1. **Single-frame messages** (vast majority): Zero-allocation — returns a slice of the shared receive buffer.
2. **Multi-frame messages** (large uploads): Accumulates frames into a `MemoryStream` until `EndOfMessage`, then processes the complete payload.

Clients do not need special handling — the WebSocket layer fragments large messages automatically.

### Upload Limits

| Constraint | Value |
|------------|-------|
| Max file size per upload | 8 MB (8,388,608 bytes) |
| Validation | Server-side in `MediaMessageHandler` |
| Error on exceed | `MediaOperationError` with descriptive message |

### Storage

Media entries are persisted using LiteDB:
- **Collection:** `media`
- **Indexes:** `Id` (unique), `FileName`

### Error Handling

| Error | Cause | Resolution |
|-------|-------|------------|
| File exceeds maximum upload size | Upload data > 8 MB | Reduce file size before uploading |
| Media not found | Invalid media ID on load/delete | Use MediaList to get valid IDs |
| Database locked | Concurrent access | Retry operation |

---

## Project Management Protocol

### Overview

The Project Management Protocol extends the base protocol to support organizing CTF work into projects. A Project groups sessions and write-up folders. Each project automatically gets a "Report" system folder on creation. Sessions link to projects via `ProjectId` and write-ups can be moved between folders via `FolderId`.

**Key Features:**
- Full CRUD for projects
- Embedded folder management (add, delete, rename)
- System folders (e.g., "Report") that cannot be deleted or renamed
- Session-to-project assignment
- Write-up-to-folder assignment
- Content listing (sessions by project, write-ups by folder)
- LiteDB persistence on server side

### Message Types

| Type | Value | Direction | Description |
|------|-------|-----------|-------------|
| ProjectCreate | 90 | Client → Server | Create a new project |
| ProjectLoad | 91 | Client → Server | Load full project data |
| ProjectList | 92 | Client → Server | List all projects (metadata) |
| ProjectUpdate | 93 | Client → Server | Update project name and description |
| ProjectDelete | 94 | Client → Server | Delete a project |
| ProjectAddFolder | 95 | Client → Server | Add a folder to a project |
| ProjectDeleteFolder | 96 | Client → Server | Delete a folder from a project |
| ProjectRenameFolder | 97 | Client → Server | Rename a folder |
| ProjectAssignSession | 98 | Client → Server | Assign/unlink a session to/from a project |
| ProjectCreateResult | 100 | Server → Client | Response to ProjectCreate |
| ProjectLoadResult | 101 | Server → Client | Response to ProjectLoad |
| ProjectListResult | 102 | Server → Client | Response to ProjectList |
| ProjectUpdateResult | 103 | Server → Client | Response to ProjectUpdate |
| ProjectDeleteResult | 104 | Server → Client | Response to ProjectDelete |
| ProjectAddFolderResult | 105 | Server → Client | Response to ProjectAddFolder |
| ProjectDeleteFolderResult | 106 | Server → Client | Response to ProjectDeleteFolder |
| ProjectRenameFolderResult | 107 | Server → Client | Response to ProjectRenameFolder |
| ProjectAssignSessionResult | 108 | Server → Client | Response to ProjectAssignSession |
| ProjectOperationError | 109 | Server → Client | Error response |
| ProjectListSessions | 110 | Client → Server | List sessions for a project |
| ProjectListSessionsResult | 111 | Server → Client | Response to ProjectListSessions |
| ProjectListWriteUps | 112 | Client → Server | List write-ups for a folder |
| ProjectListWriteUpsResult | 113 | Server → Client | Response to ProjectListWriteUps |
| ProjectExport | 114 | Client → Server | Export project to JSON file |
| ProjectExportResult | 115 | Server → Client | Response to ProjectExport |
| ProjectImport | 116 | Client → Server | Import project from JSON file |
| ProjectImportResult | 117 | Server → Client | Response to ProjectImport |

### Request Message Formats

#### ProjectCreate
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (90)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 4    | int32     | Name length (N)
21     | N    | bytes[]   | Project name (UTF-8)
21+N   | 4    | int32     | Description length (D)
25+N   | D    | bytes[]   | Description (UTF-8)
```

#### ProjectLoad
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (91)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Project ID (UUID)
```

#### ProjectList
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (92)
1      | 16   | bytes[16] | Message ID (UUID)
```

#### ProjectUpdate
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (93)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Project ID (UUID)
33     | 4    | int32     | Name length (N)
37     | N    | bytes[]   | Project name (UTF-8)
37+N   | 4    | int32     | Description length (D)
41+N   | D    | bytes[]   | Description (UTF-8)
```

#### ProjectDelete
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (94)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Project ID (UUID)
```

**Notes:**
- Deleting a project orphans all linked sessions (`ProjectId` → null) and all write-ups in the project's folders (`FolderId` → null).

#### ProjectAddFolder
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (95)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Project ID (UUID)
33     | 4    | int32     | Name length (N)
37     | N    | bytes[]   | Folder name (UTF-8)
```

#### ProjectDeleteFolder
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (96)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Project ID (UUID)
33     | 16   | bytes[16] | Folder ID (UUID)
```

**Notes:**
- System folders (e.g., "Report") cannot be deleted. The server returns `success = 0`.
- Deleting a folder orphans all write-ups in that folder (`FolderId` → null).

#### ProjectRenameFolder
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (97)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Project ID (UUID)
33     | 16   | bytes[16] | Folder ID (UUID)
49     | 4    | int32     | Name length (N)
53     | N    | bytes[]   | New folder name (UTF-8)
```

**Notes:**
- System folders cannot be renamed. The server returns `success = 0`.

#### ProjectAssignSession
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (98)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Session ID (UUID)
33     | 16   | bytes[16] | Project ID (UUID, Guid.Empty to unlink)
```

#### ProjectListSessions
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (110)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Project ID (UUID)
```

#### ProjectListWriteUps
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (112)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Folder ID (UUID)
```

#### ProjectExport
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (114)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 16   | bytes[16] | Project ID (UUID)
33     | 4    | int32     | Path length (P)
37     | P    | bytes[]   | File path (UTF-8)
37+P   | 1    | byte      | Flags bitmask (optional, default 0x1F)
```

**Flags bitmask:**
| Bit | Mask | Flag |
|-----|------|------|
| 0   | 0x01 | includeHistory — include session history entries |
| 1   | 0x02 | includeTargets — include session targets |
| 2   | 0x04 | includeWriteUps — include write-ups |
| 3   | 0x08 | includeMedia — include media blobs (ignored if writeUps=false) |
| 4   | 0x10 | includeScripts — include all custom scripts (global, not project-scoped) |

Default `0x1F` = all included. If the flags byte is absent (old client), `0x1F` is assumed.

**Notes:**
- The server writes the project data as a JSON file to the specified filesystem path.
- The export includes the project, its folders, and optionally: sessions (with history and targets), write-ups, media, and custom scripts — controlled by the flags bitmask.
- Media binary data is encoded as base64 in the JSON.
- The `scripts` field in the export JSON is optional. When present, it contains all custom scripts from the instance.

#### ProjectImport
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (116)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 4    | int32     | Path length (P)
21     | P    | bytes[]   | File path (UTF-8)
```

**Notes:**
- The server reads and parses a JSON export file from the specified filesystem path.
- All original IDs are preserved. If the project ID already exists in the database, the import is rejected.
- Collision checks are performed for all entity IDs (project, sessions, write-ups, media).

### Response Message Formats

#### ProjectCreateResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (100)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | 16   | bytes[16] | Created project ID (UUID)
```

#### ProjectLoadResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (101)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | ...  | Project   | Project data (if success)
```

**Project structure:**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Project ID (UUID)
16     | 4    | int32     | Name length (N)
20     | N    | bytes[]   | Name (UTF-8)
20+N   | 4    | int32     | Description length (D)
24+N   | D    | bytes[]   | Description (UTF-8)
24+N+D | 8    | int64     | CreatedAt (.NET ticks)
32+N+D | 8    | int64     | UpdatedAt (.NET ticks)
40+N+D | 4    | int32     | Folder count (F)
44+N+D | ...  | Folder[]  | Folders
```

**ProjectFolder structure:**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Folder ID (UUID)
16     | 4    | int32     | Name length (N)
20     | N    | bytes[]   | Name (UTF-8)
20+N   | 1    | byte      | IsSystem (1 = system folder, 0 = user folder)
```

#### ProjectListResult
```
OFFSET | SIZE | TYPE        | DESCRIPTION
0      | 1    | byte        | Message type (102)
1      | 16   | bytes[16]   | Message ID (UUID)
17     | 4    | int32       | Project count (N)
21     | ...  | Metadata[]  | Project metadata array
```

**ProjectMetadata structure:**
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 16   | bytes[16] | Project ID (UUID)
16     | 4    | int32     | Name length (N)
20     | N    | bytes[]   | Name (UTF-8)
20+N   | 4    | int32     | Description length (D)
24+N   | D    | bytes[]   | Description (UTF-8)
24+N+D | 8    | int64     | CreatedAt (.NET ticks)
32+N+D | 8    | int64     | UpdatedAt (.NET ticks)
40+N+D | 4    | int32     | Folder count
44+N+D | 4    | int32     | Session count
```

#### ProjectUpdateResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (103)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### ProjectDeleteResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (104)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### ProjectAddFolderResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (105)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | 16   | bytes[16] | Created folder ID (UUID)
```

#### ProjectDeleteFolderResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (106)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### ProjectRenameFolderResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (107)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### ProjectAssignSessionResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (108)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### ProjectListSessionsResult
```
OFFSET | SIZE | TYPE        | DESCRIPTION
0      | 1    | byte        | Message type (111)
1      | 16   | bytes[16]   | Message ID (UUID)
17     | 4    | int32       | Session count (N)
21     | ...  | Metadata[]  | SessionMetadata entries (same format as SessionListResult)
```

#### ProjectListWriteUpsResult
```
OFFSET | SIZE | TYPE        | DESCRIPTION
0      | 1    | byte        | Message type (113)
1      | 16   | bytes[16]   | Message ID (UUID)
17     | 4    | int32       | Write-up count (N)
21     | ...  | Metadata[]  | WriteUpMetadata entries (same format as WriteUpListResult)
```

#### ProjectExportResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (115)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
```

#### ProjectImportResult
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (117)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 1    | byte      | Success (1 = true, 0 = false)
18     | 16   | bytes[16] | Imported project ID (UUID)
```

#### ProjectOperationError
```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 1    | byte      | Message type (109)
1      | 16   | bytes[16] | Message ID (UUID)
17     | 4    | int32     | Error length (E)
21     | E    | bytes[]   | Error message (UTF-8)
```

### JSON Export Schema

The export file uses the following JSON structure (version 1):

```json
{
  "version": 1,
  "exportedAt": "2026-03-19T14:30:00Z",
  "project": {
    "id": "guid",
    "name": "string",
    "description": "string",
    "createdAt": "datetime",
    "updatedAt": "datetime",
    "folders": [
      { "id": "guid", "name": "string", "isSystem": true }
    ]
  },
  "sessions": [
    {
      "id": "guid",
      "name": "string",
      "description": "string",
      "createdAt": "datetime",
      "updatedAt": "datetime",
      "projectId": "guid",
      "history": [
        {
          "id": "guid",
          "timestamp": "datetime",
          "workingDirectory": "string",
          "command": "string",
          "output": "string",
          "exitCode": 0
        }
      ],
      "targets": [
        {
          "id": "guid",
          "address": "string",
          "port": null,
          "name": "string",
          "description": "string",
          "type": 1
        }
      ]
    }
  ],
  "writeUps": [
    {
      "id": "guid",
      "sessionId": "guid",
      "folderId": "guid-or-null",
      "name": "string",
      "content": "string",
      "createdAt": "datetime",
      "updatedAt": "datetime"
    }
  ],
  "media": [
    {
      "id": "guid",
      "fileName": "string",
      "mimeType": "string",
      "data": "base64-string",
      "createdAt": "datetime"
    }
  ]
}
```

**Notes:**
- The `version` field allows future schema evolution.
- Media `data` is base64-encoded binary.
- Media references are discovered by parsing write-up content for `media://{uuid}` patterns.

### Storage

Projects are persisted using LiteDB:
- **Collection:** `projects`
- **Indexes:** `Id` (unique), `Name`
- Folders are embedded in the project document (not a separate collection)

### Design Decisions

- **"Report" folder**: Auto-created on project creation (`IsSystem = true`). Cannot be deleted or renamed.
- **"Sessions" is virtual**: Sessions link to projects via `ProjectId`, no folder entity needed.
- **Project delete**: Orphans sessions (`ProjectId` → null) and write-ups in all project folders (`FolderId` → null).
- **Folder delete**: Orphans write-ups (`FolderId` → null).
- **Backward compatible**: `ProjectId` and `FolderId` are nullable. Existing LiteDB documents get `null` automatically.

### Error Handling

| Error | Cause | Resolution |
|-------|-------|------------|
| Project not found | Invalid project ID | Use ProjectList to get valid IDs |
| Cannot delete system folder | Attempted to delete "Report" | System folders are protected |
| Cannot rename system folder | Attempted to rename "Report" | System folders are protected |
| Session not found | Invalid session ID on assign | Use SessionList to get valid IDs |
| Project already exists | Import with existing project ID | Delete existing project first or use a different export |
| File not found | Import path does not exist | Check the file path |
| Unsupported export version | Export file version != 1 | Use a compatible export file |
| Entity already exists | Session/WriteUp/Media ID collision on import | Entities from previous import still in database |
| Database locked | Concurrent access | Retry operation |
