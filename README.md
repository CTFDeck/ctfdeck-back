# CTFDeck Backend

CTFDeck Backend is the WebSocket server and execution layer for CTFDeck. It runs terminal commands, streams output to connected clients, persists workspace data, manages tools, and exposes the binary protocol used by the CTFDeck frontend.

The frontend lives in [`ctfdeck-app`](https://github.com/CTFDeck/ctfdeck-app).

---

## Status

**Current stage: feature-complete milestone build**

The backend now supports the full application workflow: terminal execution, interactive stdin, sessions, targets, custom scripts, write-ups, media, projects, import/export, tool inventory, and installation flows.

---

## Key Features

- WebSocket binary protocol on `ws://localhost:42712` by default.
- Real-time command execution with streamed stdout/stderr.
- Interactive command input through `CommandInput`.
- Ctrl+C and Ctrl+D command signals.
- Sudo password request/response flow.
- Pseudo-terminal support for interactive Unix commands that require a TTY.
- UTF-8 process output handling and terminal color environment setup.
- Persistent sessions, command history, and targets.
- Custom script CRUD and reusable command templates.
- Write-up CRUD with Markdown content.
- Media storage for write-up assets.
- Project CRUD, folders, session/write-up assignment, import, and export.
- Tool catalog, detection, install, uninstall, and progress reporting.
- Multi-client WebSocket support.
- LiteDB-backed local persistence.

---

## Solution Projects

| Project | Purpose |
|---------|---------|
| `CtfDeck.WsServer` | WebSocket host and message dispatcher |
| `CtfDeck.Terminal` | Terminal execution, services, handlers, tool/project/session features |
| `CtfDeck.Contracts` | Transport protocol, DTOs, serializers |
| `CtfDeck.Data` | LiteDB persistence models and repositories |
| `CtfDeck.Abstractions` | Repository/tool/session port interfaces |
| `CtfDeck.Tests` | Unit and integration tests |

---

## Requirements

- .NET SDK 8.0
- Git
- Platform shell/toolchain:
  - Windows: `cmd.exe`, PowerShell, or Git Bash depending on environment
  - Linux/macOS: Bash preferred
- `script` command on Linux/macOS for pseudo-terminal support when required by interactive sudo commands

---

## Quick Start

```bash
dotnet restore
dotnet build
dotnet run --project src/CtfDeck.WsServer/
```

The server listens on:

```text
ws://localhost:42712
```

For development with reload:

```bash
dotnet watch --project src/CtfDeck.WsServer/
```

---

## Tests

```bash
dotnet test
```

With coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Documentation

- [User Guide](docs/USER.md)
- [Developer Guide](docs/DEVELOPMENT.md)
- [Project Structure](docs/PROJECT_STRUCTURE.md)
- [Protocol Specification](docs/PROTOCOL.md)
- [Database Documentation](docs/DATABASE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Contributing](docs/CONTRIBUTING.md)

---

## Notes

- The WebSocket protocol is binary and UUID-correlated.
- Most user-facing operations are initiated by the frontend; this server does not provide a REST API.
- Command behavior depends on the host OS, installed tools, shell, permissions, and network namespace.
