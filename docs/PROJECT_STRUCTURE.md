# Project Structure

This document provides an overview of the CTFDeck Backend repository structure, explaining the purpose of the key directories and files.

## Root Directory

- **`src/`**: Contains the source code for the platform.
- **`tests/`**: Contains the automated test suites.
- **`docs/`**: Project documentation (Guides, Policies, etc.).
- **`ctfdeck-back.sln`**: The main .NET solution file.

---

## Source Code (`src/`)

The backend is currently consolidated into a main project:

### [CtfDeck.Terminal](file:///home/binosspc/Documents/tek4/Capstone/CTFDeck/ctfdeck-back/src/CtfDeck.Terminal)

This is the core of the backend server. It implements the WebSocket host and coordinates command execution.

- **`WebSocket/`**: Implementation of the binary WebSocket server and message dispatching.
- **`Terminal/`**: Logic for interacting with the system shell (cmd, sh, bash) and streaming output.
- **`Session/`**: Persistence layer using LiteDB to record command history and manage targets.
- **`Script/`**: Management of custom scripts and command templates.
- **`Dto/`**: Data Transfer Objects for binary serialization.
- **`Program.cs`**: Entry point of the application.

---

## Tests (`tests/`)

- **[CtfDeck.Tests](file:///home/binosspc/Documents/tek4/Capstone/CTFDeck/ctfdeck-back/tests/CtfDeck.Tests)**: Unit and integration tests for the Terminal project.
  - **`Terminal/`**: Tests for shell interaction and command parsing.
  - **`WebSocket/`**: Tests for the binary protocol and connection handling.

---

## Documentation (`docs/`)

- **`USER.md`**: Main guide for installation and server operation.
- **`DEVELOPMENT.md`**: Entry point for developers.
- **`CONTRIBUTING.md`**: Guidelines for contributors.
- **`PROTOCOL.md`**: Deep dive into the custom binary WebSocket protocol.
- **`COMMIT_POLICY.md`**, **`BRANCHES_POLICY.md`**, etc.: Governance and workflow policies.

---

## Build and Output

- **`bin/`** and **`obj/`**: Standard .NET build output directories (gitignored).
- **`ctfdeck_sessions.db`**: LiteDB database file created at runtime to store session data.
