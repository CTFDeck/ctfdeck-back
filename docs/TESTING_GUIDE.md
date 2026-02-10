# Testing Guide

## Running Tests

The CTFDeck project includes a comprehensive test suite located in the `tests/CtfDeck.Tests/` directory. The tests are built using xUnit and Microsoft.NET.Test.Sdk.

### Using the .NET CLI

To run all tests in the solution:

```bash
dotnet test
```

To run tests from a specific project:

```bash
dotnet test tests/CtfDeck.Tests/
```

To run tests with detailed output:

```bash
dotnet test tests/CtfDeck.Tests/ --logger "console;verbosity=detailed"
```

### Using Visual Studio

1. Open the `CtfDeck.sln` solution file in Visual Studio
2. Open the Test Explorer window (Test → Test Explorer)
3. Click "Run All" to execute all tests
4. Or right-click on specific test classes/methods to run individual tests

### Using Visual Studio Code

1. Open the project folder in Visual Studio Code
2. Install the C# extension if not already installed
3. Open the Command Palette (Ctrl+Shift+P)
4. Type "Testing: Run All Tests" and execute the command

## Test Project Configuration

The test project (`tests/CtfDeck.Tests/CtfDeck.Tests.csproj`) is configured with:

- **Target Framework**: net8.0
- **Test Framework**: xUnit
- **Test SDK**: Microsoft.NET.Test.Sdk
- **Assertions**: FluentAssertions
- **Code Coverage**: coverlet.collector

### Dependencies

The test project includes the following key packages:

- `Microsoft.NET.Test.Sdk`: Provides the test execution framework
- `xunit`: Unit testing framework
- `xunit.runner.visualstudio`: Test runner for Visual Studio
- `FluentAssertions`: Provides fluent assertion methods
- `coverlet.collector`: Collects code coverage data

## Running Tests with Code Coverage

To run tests and generate a code coverage report:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

This will generate coverage reports in the `TestResults` directory.

## Filtering Tests

You can run specific tests using filters:

```bash
# Run tests by trait
dotnet test --filter "TestCategory=Unit"

# Run tests by name
dotnet test --filter "FullyQualifiedName~MyTestClass.MyTestMethod"

# Run tests in a specific assembly
dotnet test --test-adapter-path:. --logger:trx
```

## Continuous Integration

The test suite is designed to run in CI/CD pipelines. The project includes GitHub Actions workflows (in `.github/workflows/`) that automatically run tests on push and pull requests.