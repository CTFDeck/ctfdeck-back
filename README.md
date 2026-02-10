# CTFDeck Backend

CTFDeck Backend is the server component of the CTFDeck platform, providing a WebSocket-based execution engine for cybersecurity tools and remote shell commands.

## Key Features

- **WebSocket Server**: Low-latency binary communication for real-time command output.
- **Shell Execution**: Flexible command execution on Windows, Linux, and macOS.
- **Protocol Management**: Structured message correlation using UUIDs.
- **Multi-Client Support**: Handles multiple concurrent client connections.

## Documentation

- [**User Guide**](docs/USER.md): Installation, configuration, and how to run the server.
- [**Developer Guide**](docs/DEVELOPMENT.md): Project structure, coding standards, and development workflow.
- [**Project Structure**](docs/PROJECT_STRUCTURE.md): Detailed overview of the repository and file organization.
- [**Contributing**](docs/CONTRIBUTING.md): How to contribute to the project.
- [**Protocol Specification**](docs/PROTOCOL.md): Details about the binary communication protocol.

## Quick Start

```bash
dotnet restore
dotnet build
dotnet run --project src/CtfDeck.Terminal/
```

For more details, see the [User Guide](docs/USER.md).

## Running Tests

```bash
dotnet test --collect:"XPlat Code Coverage"
```