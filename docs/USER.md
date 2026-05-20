# CTFDeck Backend - User Guide

CTFDeck Backend is the local WebSocket server used by CTFDeck App. It executes commands, streams terminal output, stores workspace data, manages tools, and handles project import/export.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Terminal Execution](#terminal-execution)
- [Persistence](#persistence)
- [Tools](#tools)
- [Import and Export](#import-and-export)
- [Protocol Documentation](#protocol-documentation)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git
- A supported OS shell:
  - Windows: `cmd.exe`, PowerShell, or Git Bash depending on environment
  - Linux/macOS: Bash preferred
- Linux/macOS: `script` available on `PATH` for pseudo-terminal support with interactive sudo commands

If you use .NET 8, project language version should remain compatible with C# 12. A .NET 8 SDK will not compile projects forced to `LangVersion=13.0`.

---

## Installation

Clone and build:

```bash
git clone https://github.com/CTFDeck/ctfdeck-back.git
cd ctfdeck-back
dotnet restore
dotnet build
```

---

## Quick Start

Run the WebSocket server:

```bash
dotnet run --project src/CtfDeck.WsServer/
```

Development watch mode:

```bash
dotnet watch --project src/CtfDeck.WsServer/
```

The server listens on:

```text
ws://localhost:42712
```

Start CTFDeck App and connect to that URL.

---

## Configuration

Default behavior:

| Setting | Value |
|---------|-------|
| WebSocket URL | `ws://localhost:42712` |
| Framework | `.NET 8.0` |
| Persistence | LiteDB local database |
| Command timeout | 5 minutes |
| Media upload limit | 8 MB per file |

The server is designed for local or trusted-network use. It executes commands on the host machine, so do not expose it directly to untrusted networks.

---

## Terminal Execution

The backend executes commands through the resolved platform shell and streams output in real time.

Supported terminal behavior:

- Standard command execution.
- stdout/stderr streaming.
- ANSI color environment setup.
- UTF-8 output decoding.
- Current working directory tracking.
- `cd` handling.
- Command cancellation.
- EOF signaling.
- Raw stdin forwarding to running commands.
- Sudo password request/response.
- Pseudo-terminal execution for Unix commands that require a TTY.

Interactive examples:

```bash
sudo apt remove nmap
cat
python3 -
```

When the frontend sends input for an active command, the backend writes it to the command stdin. Ctrl+C cancels the command; Ctrl+D closes stdin.

### Platform Notes

Command output and network scan results depend on where the backend runs. Running the backend from Windows, Git Bash, WSL, Linux, or macOS may produce different behavior because each environment sees a different shell, PATH, permissions, network namespace, and installed toolchain.

---

## Persistence

The backend persists workspace data with LiteDB.

Stored data includes:

- Sessions
- Command history
- Targets
- Custom scripts
- Write-ups
- Media
- Projects
- Project folders and assignments

The database file is created locally by the backend. See [Database Documentation](DATABASE.md) for schema details.

---

## Tools

The backend owns tool inventory and installation logic.

Supported tool features:

- Tool catalog loaded from backend resources.
- Installed/missing tool detection.
- Tool path resolution.
- Install/uninstall workflows.
- Progress events sent to the frontend.
- Sudo password request for privileged operations.

Tool installation support depends on the host OS and each tool definition.

---

## Import and Export

CTFDeck supports exporting project workspaces to JSON and importing them later.

Export can include:

| Flag | Mask | Meaning |
|------|------|---------|
| `includeHistory` | `0x01` | Session command history |
| `includeTargets` | `0x02` | Session targets |
| `includeWriteUps` | `0x04` | Write-ups |
| `includeMedia` | `0x08` | Media blobs used by write-ups |
| `includeScripts` | `0x10` | Custom scripts |

Default export mask: `0x1F`.

Import behavior:

- Preserves original IDs.
- Rejects imports when project/session/write-up/media IDs already exist.
- Requires supported export schema version.
- Imports custom scripts when present.

For the JSON schema, see [Protocol Documentation](PROTOCOL.md#json-export-schema).

---

## Protocol Documentation

The frontend communicates with the backend through a binary WebSocket protocol. Messages are type-prefixed and correlated with UUIDs.

Major protocol areas:

- Terminal command execution and streaming
- Command input and command signals
- Sudo password request/provide
- Sessions and targets
- Custom scripts
- Write-ups
- Media
- Projects
- Tools
- Import/export

See [Protocol Specification](PROTOCOL.md) for message layouts.

---

## Troubleshooting

### `.NET SDK not found`

Install .NET SDK 8.0 and ensure `dotnet` is in your `PATH`.

### `Invalid option '13.0' for /langversion`

You are compiling with .NET SDK 8. Use `LangVersion=12.0` or remove the explicit language version.

### Client cannot connect to `ws://localhost:42712`

Check:

1. The backend process is running.
2. No other process is using port `42712`.
3. Firewall rules allow local WebSocket connections.
4. The frontend is using the correct server URL.

### Build fails because `.pdb` is locked or access is denied

Stop any running `dotnet watch`, backend server, IDE debug session, or test process that may hold files in `bin/` or `obj/`, then rebuild.

### Commands fail or time out

Commands have a default timeout. Long scans or brute force jobs should be split or run with appropriate backend limits. Also verify the backend host has the required permissions and tools installed.

### Sudo or confirmation prompts abort immediately

Ensure the backend includes pseudo-terminal support changes and that `script` is available on Linux/macOS. Some commands require a TTY and will not behave correctly with plain stdin pipes.

### Output encoding is broken

The backend forces UTF-8 stdout/stderr decoding and `C.UTF-8` locale where possible. If a tool emits another encoding, its output may still appear incorrectly.

### Nmap output differs between Git Bash and WSL

This is usually environmental, not a protocol parse issue. Git Bash, Windows, Docker Desktop, and WSL can see different network stacks and service responses on `127.0.0.1`, so `nmap -sV` may classify ports differently or print service fingerprints in one environment but not another.

---

## Tests

Run:

```bash
dotnet test
```

With coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```
