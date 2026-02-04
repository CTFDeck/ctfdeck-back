# Project Setup Guide

## Prerequisites

Before setting up the CTFDeck project, ensure you have the following prerequisites installed on your system:

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- A code editor (Visual Studio, Visual Studio Code, or JetBrains Rider)
- Git for version control

## Installing .NET SDK

### Windows
1. Download the .NET SDK 8.0 or later from the [official .NET download page](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Run the installer and follow the installation wizard
3. Restart your command prompt or terminal to ensure the `dotnet` command is available

### macOS
1. Install using Homebrew:
   ```bash
   brew install --cask dotnet-sdk
   ```
2. Or download from the [official .NET download page](https://dotnet.microsoft.com/download/dotnet/8.0)

### Linux
Follow the instructions for your specific distribution on the [official .NET download page](https://dotnet.microsoft.com/download/dotnet/8.0)

## Cloning the Repository

```bash
git clone https://github.com/your-username/CTFDeck-repo.git
cd ctfdeck-back
```

## Restoring Dependencies

After cloning the repository, restore the project dependencies using the following command:

```bash
dotnet restore
```

This command downloads and installs all the NuGet packages referenced by the projects in the solution.

## Verifying Installation

To verify that everything is set up correctly, you can build the solution:

```bash
dotnet build
```

If the build completes successfully without errors, your setup is complete.