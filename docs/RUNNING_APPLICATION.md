# Running the Application

## Running the CtfDeck Terminal Application

The main application is located in the `src/CtfDeck.Terminal/` directory. Here's how to run it:

### Using the .NET CLI

From the project root directory, run:

```bash
dotnet run --project src/CtfDeck.Terminal/
```

Alternatively, navigate to the project directory and run:

```bash
cd src/CtfDeck.Terminal/
dotnet run
```

### Using Visual Studio

1. Open the `CtfDeck.sln` solution file in Visual Studio
2. Right-click on the `CtfDeck.Terminal` project in Solution Explorer
3. Select "Set as Startup Project"
4. Press F5 or click the "Start" button to run the application

### Using Visual Studio Code

1. Open the project folder in Visual Studio Code
2. Press Ctrl+Shift+P to open the command palette
3. Type "Tasks: Run Task" and select it
4. Choose ".NET: run" from the list of tasks

## Application Configuration

The CtfDeck Terminal application is configured through the `CtfDeck.Terminal.csproj` file. Key configuration options include:

- **Target Framework**: net8.0
- **Output Type**: Exe (console application)
- **Implicit Usings**: Enabled
- **Nullable**: Enabled

## WebSocket Server

The application implements a WebSocket server that listens for binary commands and responds with execution results. The server handles:

- Command execution via shell
- Binary message serialization
- Message correlation using UUIDs
- Multi-client support

For detailed information about the protocol, see the [Protocol Documentation](PROTOCOL.md).