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