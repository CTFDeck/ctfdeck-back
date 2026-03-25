# CTFDeck Backend - User Guide

This guide provides instructions for installing, configuring, and running the CTFDeck Backend server.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)
- [Protocol Documentation](#protocol-documentation)

---

## Prerequisites

Before setting up the CTFDeck Backend, ensure you have the following installed:

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Git for version control
- A code editor (Visual Studio, VS Code, or JetBrains Rider)

---

## Installation

### 1. Clone the repository

```bash
git clone https://github.com/CTFDeck/ctfdeck-back.git
cd ctfdeck-back
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Build the solution

```bash
dotnet build
```

---

## Quick Start

### Running the server

The main application is located in `src/CtfDeck.WsServer/`.

#### Using the .NET CLI

From the root directory:
```bash
dotnet run --project src/CtfDeck.WsServer/
```

#### Using Visual Studio
1. Open `CtfDeck.sln`.
2. Set `CtfDeck.WsServer` as the startup project.
3. Press `F5` to start.

The server will start and listen for WebSocket connections on port `42712` by default.

---

## Configuration

The backend application is configured primarily through project settings and environment variables. Key aspects include:

- **Port**: 42712 (default WebSocket port)
- **Framework**: .NET 8.0
- **Shell**: The backend executes commands via the system shell (cmd.exe on Windows, sh/bash on Linux/macOS).

For detailed technical configuration, refer to the project file: `src/CtfDeck.Terminal/CtfDeck.Terminal.csproj`.

---

## Troubleshooting

### Common Issues

#### .NET SDK Not Found
- **Problem**: `dotnet` command is not recognized.
- **Solution**: Verify .NET 8.0 installation and ensure it's in your system `PATH`. Restart your terminal.

#### WebSocket Connection Issues
- **Problem**: Client cannot connect to `ws://localhost:42712`.
- **Solution**: 
  - Ensure the backend is actually running.
  - Check if another process is using port 42712.
  - Check firewall settings.

#### Command Execution Errors
- **Problem**: Commands fail or time out.
- **Solution**: The backend has a default timeout for commands. Ensure your environment has the necessary permissions to execute the requested shells.

For more details, see [Troubleshooting Guide](TROUBLESHOOTING.md).

---

## Import / Export

CTFDeck supports exporting a complete project to a JSON file and re-importing it later (or on another machine).

### Export a project

The export request accepts a **flags bitmask** (1 byte) to control what is included:

| Bit | Mask | Flag |
|-----|------|------|
| 0   | 0x01 | includeHistory — session history entries |
| 1   | 0x02 | includeTargets — session targets |
| 2   | 0x04 | includeWriteUps — write-ups |
| 3   | 0x08 | includeMedia — media blobs (ignored if writeUps is off) |
| 4   | 0x10 | includeScripts — all custom scripts from the instance |

Default: `0x1F` (everything included). If the flags byte is absent, all content is exported.

### Import a project

The import processes everything present in the JSON file.

**Notes:**
- All original IDs are preserved.
- If the project ID (or any session/write-up/media ID) already exists in the database, the import is rejected.
- The export file must be version 1.
- If the export contains custom scripts, they are imported as well.

For the full JSON schema, see [Protocol Documentation](PROTOCOL.md#json-export-schema).

---

## Protocol Documentation

The backend uses a specific binary protocol for communication. For detailed information on message formats, UUID correlation, and serialization, see the [Protocol Documentation](PROTOCOL.md).
