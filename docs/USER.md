# CTFDeck Backend - User Guide

CTFDeck Backend is a WebSocket server that executes shell commands remotely. It provides real-time streaming output with ANSI color support and is designed to work with the CTFDeck App frontend.

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Features](#features)
- [Troubleshooting](#troubleshooting)

---

## Installation

### Prerequisites

- .NET 8.0 SDK (for building from source)
- Or download the precompiled executable

### Option 1: Download the executable

Download the latest release from [GitHub Releases](https://github.com/CTFDeck/ctfdeck-back/releases):

| Platform | Archive |
|----------|---------|
| Linux x64 | `release-linux-x64.zip` |
| Windows x64 | `release-win-x64.zip` |
| macOS Intel | `release-osx-x64.zip` |
| macOS ARM | `release-osx-arm64.zip` |

Extract the archive to get the `CtfDeck.Terminal` executable (`CtfDeck.Terminal.exe` on Windows).

### Option 2: Build from source

```bash
git clone https://github.com/CTFDeck/ctfdeck-back.git
cd ctfdeck-back
dotnet restore
dotnet build -c Release
```

---

## Quick Start

### Start the server

```bash
# With the executable
./CtfDeck.Terminal           # Linux/macOS
CtfDeck.Terminal.exe         # Windows

# Or from source
dotnet run --project src/CtfDeck.Terminal
```

The server starts on `localhost:42712` by default.

### Verify the server is running

You should see output similar to:

```
WebSocket server started on http://localhost:42712/
Press Ctrl+C to stop the server...
```

---

## Configuration

### Default Settings

| Parameter | Default Value |
|-----------|---------------|
| Host | `localhost` |
| Port | `42712` |
| Command timeout | 5 minutes |

### Network Binding

The server binds to `localhost` only by default for security reasons. It should not be exposed directly to the network without proper security measures.

---

## Features

### Command Execution

Execute any shell command through the WebSocket connection. Commands are executed in the detected system shell.

### Real-time Streaming

Command output is streamed in real-time to the frontend as it is produced.

### Directory Navigation

The server maintains working directory state per client session:

```bash
cd /home/user    # Change directory
pwd              # Get current directory
```

### Color Support

Common commands are automatically enhanced with color flags:
- `ls`, `grep`, `diff`, `tree`

Colors are displayed in the CTFDeck App frontend.

### Multi-Client Support

Multiple clients can connect simultaneously. Each client has an isolated terminal session with its own working directory.

### Shell Detection

The server automatically detects and uses the appropriate shell:

| Platform | Detected Shells |
|----------|-----------------|
| Windows | Cmd, PowerShell, Bash (via Git Bash or WSL) |
| Linux/macOS | Bash |

---

## Troubleshooting

### Server won't start

**Error: Port already in use**

Another process is using port 42712.

```bash
# Find the process (Linux/macOS)
lsof -i :42712

# Find the process (Windows)
netstat -ano | findstr :42712
```

**Error: Permission denied**

On Linux/macOS, ensure the executable has run permissions:

```bash
chmod +x CtfDeck.Terminal
```

### Commands timeout

Commands have a 5-minute timeout. For long-running processes, consider splitting them into smaller operations.

### No color output

If colors are not displaying:
1. Ensure you are using CTFDeck App frontend
2. Some commands may not support color output

### Connection refused

1. Verify the server is running
2. Check the correct port (42712)
3. Ensure no firewall is blocking the connection

---

## Security Considerations

### Local Use Only

The server is designed for **local use only**. It:
- Has no authentication
- Has no authorization
- Executes commands with the server's user permissions

### Network Deployment

If network access is required:
- Use an SSH tunnel
- Deploy behind a VPN
- Never expose directly to the Internet

### Command Execution

All commands run with the permissions of the user running the server. Be cautious about:
- Commands that modify system files
- Commands that could expose sensitive data
- Long-running or resource-intensive commands

---

## Supported Platforms

| Platform | Architecture | Status |
|----------|--------------|--------|
| Windows 10+ | x64 | Supported |
| Linux | x64 | Supported |
| macOS | x64 (Intel) | Supported |
| macOS | ARM64 (Apple Silicon) | Supported |

---

## Limits

| Limit | Value |
|-------|-------|
| Command timeout | 5 minutes |
