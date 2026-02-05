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

### Command Message

Clients send commands using the following binary structure:

```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 4    | int32     | Command length (N)
4      | N    | bytes[]   | Command string (UTF-8)
4+N    | 16   | bytes[16] | Message ID (UUID)
```

**Total Size:** 4 + N + 16 bytes

### Response Message

Servers respond with the following binary structure:

```
OFFSET | SIZE | TYPE      | DESCRIPTION
0      | 4    | int32     | Exit code
4      | 4    | int32     | Output length (M)
8      | M    | bytes[]   | Output string (UTF-8)
8+M    | 4    | int32     | Error length (K)
8+M+K  | K    | bytes[]   | Error string (UTF-8)
8+M+K+4|16   | bytes[16] | Message ID (UUID)
```

**Total Size:** 8 + M + K + 16 bytes

## Message Types

### Command Message

Sent by clients to execute shell commands.

**Fields:**
- `command_length`: Length of the command string in bytes
- `command_bytes`: UTF-8 encoded command string
- `message_id`: UUID for request-response correlation

### Response Message

Sent by servers with command execution results.

**Fields:**
- `exit_code`: Process exit code (0 = success, non-zero = error)
- `output_length`: Length of stdout output in bytes
- `output_bytes`: UTF-8 encoded stdout output
- `error_length`: Length of stderr output in bytes
- `error_bytes`: UTF-8 encoded stderr output
- `message_id`: UUID matching the original command

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
| SessionUpdateTargets | 15 | Client → Server | Sync targets to session |
| SessionUpdate | 16 | Client → Server | Update session name and description |
| SessionCreateResult | 20 | Server → Client | Response to SessionCreate |
| SessionSetActiveResult | 21 | Server → Client | Response to SessionSetActive |
| SessionLoadResult | 22 | Server → Client | Response to SessionLoad |
| SessionListResult | 23 | Server → Client | Response to SessionList |
| SessionDeleteResult | 24 | Server → Client | Response to SessionDelete |
| SessionUpdateResult | 25 | Server → Client | Response to SessionUpdate |
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
28+A+N | 4    | int32     | Target type (enum)
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
40+N+D | 4    | int32     | History count (H)
44+N+D | ...  | Entry[]   | History entries
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
