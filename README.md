# CtfDeck Backend

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp&logoColor=white)
![xUnit](https://img.shields.io/badge/xUnit-2.5-512BD4?logo=dotnet&logoColor=white)
![FluentAssertions](https://img.shields.io/badge/FluentAssertions-8.8-blue)

## Overview

CtfDeck Backend is a .NET 8 terminal execution service that provides remote command execution capabilities over WebSocket connections. It enables clients to execute shell commands and receive real-time streaming output through a high-performance binary protocol.

## Technology Stack

- **.NET 8** - Runtime and SDK
- **C# 12** - Programming language
- **xUnit** - Testing framework
- **FluentAssertions** - Test assertions

## Project Structure

```
ctfdeck-back/
+-- CtfDeck.sln                          # Solution file
+-- src/
|   +-- CtfDeck.Terminal/                # Main project
|       +-- Program.cs                   # Application entry point
|       +-- Dto/                         # Data Transfer Objects
|       +-- Terminal/                    # Terminal execution logic
|       |   +-- Core/                    # Core models
|       |   +-- Execution/               # Command execution
|       |   +-- Navigation/              # Directory navigation
|       |   +-- Shell/                   # Shell detection
|       +-- WebSocket/                   # WebSocket communication
+-- tests/
|   +-- CtfDeck.Tests/                   # Unit tests
+-- docs/                                # Documentation
```

### Layer Descriptions

#### WebSocket Layer (`WebSocket/`)

Handles client connections and binary message serialization.

| Component | Description |
|-----------|-------------|
| `WebSocketServer` | Manages client connections and message routing |
| `BinaryProtocol` | Serialization/deserialization of binary messages |
| `OutputBatcher` | Batches streaming output for performance |

#### Terminal Layer (`Terminal/`)

Orchestrates command execution and terminal sessions.

| Component | Description |
|-----------|-------------|
| `TerminalExecutor` | Main entry point for command execution |
| `TerminalService` | Interactive REPL terminal service |

#### Execution Layer (`Terminal/Execution/`)

Handles actual command execution and preprocessing.

| Component | Description |
|-----------|-------------|
| `ProcessRunner` | Executes commands in shell processes with streaming output |
| `CommandPreprocessor` | Prepares commands (color support, shell-specific formatting) |

#### Core Layer (`Terminal/Core/`, `Terminal/Navigation/`, `Terminal/Shell/`)

Foundational components and models.

| Component | Description |
|-----------|-------------|
| `CommandResult` | Immutable command execution result |
| `DirectoryNavigator` | Handles `cd` commands and path resolution |
| `ShellDetector` | Detects available shells (Bash, Cmd, PowerShell) |

## Key Features

### Multi-Shell Support

The backend automatically detects and supports multiple shells:

- **Bash** (preferred on Unix, available on Windows via Git Bash)
- **PowerShell**
- **Cmd** (Windows fallback)

### Streaming Output

Commands output is streamed in real-time using a batched approach:

- Output is batched every 5ms or when buffer reaches 8KB
- Separate streams for stdout and stderr
- Exit code and working directory sent on completion

### Cross-Platform

- Runs on Windows, Linux, and macOS
- Automatic shell detection based on platform
- Path handling compatible with both Unix and Windows

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project src/CtfDeck.Terminal
```

The server starts on `localhost:42712` by default.


## Run tests 

```
dotnet test --collect:"XPlat Code Coverage" && reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html && start coveragereport/index.html
```

## Configuration

The server configuration is set in `Program.cs`:

| Setting | Default | Description |
|---------|---------|-------------|
| Host | `localhost` | Server bind address |
| Port | `42712` | WebSocket port |

## Related Documentation

For specific topics, refer to the following documentation:

| Document | Description |
|----------|-------------|
| [PROTOCOL.md](./PROTOCOL.md) | WebSocket binary protocol specification |
| [BRANCHES_POLICY.md](./BRANCHES_POLICY.md) | Git branching strategy |
| [COMMIT_POLICY.md](./COMMIT_POLICY.md) | Commit message conventions |
| [MERGE_POLICY.md](./MERGE_POLICY.md) | Pull request and merge guidelines |
| [BRANCHES_UPDATE_POLICY.md](./BRANCHES_UPDATE_POLICY.md) | Branch update procedures |
