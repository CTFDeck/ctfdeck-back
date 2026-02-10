# CTFDeck Backend - Developer Guide

Welcome to the CTFDeck Backend development documentation. This project is built with .NET 8.0 and follows a modular architecture.

## Getting Started

If you haven't already, please follow the [User Guide](USER.md) to set up your local environment and run the application.

## Project Structure

For a detailed overview of the repository and the purpose of each directory, see the [Project Structure Guide](PROJECT_STRUCTURE.md).

The solution consists of several key projects:

- **src/CtfDeck.Terminal**: The main entry point and WebSocket server implementation.
- **tests/CtfDeck.Tests**: Unit and integration tests using xUnit and FluentAssertions.

## Development Workflow

### Coding Standards

We follow standard C# and .NET coding conventions. Key points:
- Use File-Scoped Namespaces.
- Follow SOLID principles.
- Ensure all public methods are documented.

### Building and Testing

Always ensure the project builds and all tests pass before submitting a pull request.

```bash
dotnet build
dotnet test
```

### Git Policies

To maintain a clean and consistent project history, we enforce specific policies:

- [Commit Policy](COMMIT_POLICY.md): Angular/Conventional commit format.
- [Branches Policy](BRANCHES_POLICY.md): Branch naming and lifecycle.
- [Merge Policy](MERGE_POLICY.md): Rules for merging code.

## Protocol Implementation

If you are working on the communication layer, please refer to the [Protocol Documentation](PROTOCOL.md).

## Contributing

Interested in contributing? Check out our [Contributing Guide](CONTRIBUTING.md).
