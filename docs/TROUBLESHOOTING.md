# Troubleshooting Guide

## Common Issues and Solutions

### .NET SDK Not Found

**Problem**: Command `dotnet` is not recognized or not found.

**Solution**:
1. Verify that .NET SDK 8.0 or later is installed by visiting the [official .NET download page](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Check your PATH environment variable includes the .NET SDK installation directory
3. Restart your command prompt or terminal after installing the SDK
4. On Windows, you may need to restart your computer after installation

### Dependency Restore Issues

**Problem**: `dotnet restore` fails with package resolution errors.

**Solution**:
1. Clear the local NuGet cache:
   ```bash
   dotnet nuget locals all --clear
   ```
2. Verify your internet connection
3. Check if you're behind a corporate firewall that might block NuGet package sources
4. Try restoring with verbose output to see detailed error information:
   ```bash
   dotnet restore --verbosity detailed
   ```

### Build Errors

**Problem**: `dotnet build` fails with compilation errors.

**Solution**:
1. Ensure all dependencies are restored: `dotnet restore`
2. Clean the solution before building: `dotnet clean`
3. Try building with detailed output: `dotnet build --verbosity detailed`
4. Check that your .NET SDK version matches the project requirements (net8.0)

### Test Failures

**Problem**: Tests fail when running `dotnet test`.

**Solution**:
1. Ensure the main application builds successfully first: `dotnet build`
2. Run tests with detailed output to see specific failure reasons:
   ```bash
   dotnet test --logger "console;verbosity=detailed"
   ```
3. Check if the test project references the correct version of the main project
4. Verify that all required dependencies are properly referenced

### WebSocket Connection Issues

**Problem**: Cannot connect to the WebSocket server or commands are not executing.

**Solution**:
1. Verify the server is running and listening on the correct port
2. Check firewall settings that might block WebSocket connections
3. Ensure the client is sending properly formatted binary messages as defined in the [Protocol Documentation](PROTOCOL.md)
4. Verify that the UUID correlation is working correctly between request and response

### Performance Issues

**Problem**: Application runs slowly or consumes excessive resources.

**Solution**:
1. Monitor resource usage during execution
2. Check for memory leaks in long-running processes
3. Optimize binary serialization/deserialization if handling large amounts of data
4. Consider implementing connection pooling for multiple concurrent clients

## Additional Resources

### .NET Documentation
- [Official .NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [.NET Command Line Interface (CLI)](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [Troubleshooting .NET Installation](https://docs.microsoft.com/en-us/dotnet/core/install/troubleshoot)

### Development Tools
- [Visual Studio Documentation](https://docs.microsoft.com/en-us/visualstudio/)
- [Visual Studio Code C# Extension](https://code.visualstudio.com/docs/languages/csharp)
- [JetBrains Rider Documentation](https://www.jetbrains.com/help/rider/)

### Community Support
- [Stack Overflow .NET Tag](https://stackoverflow.com/questions/tagged/.net)
- [.NET Community Discord](https://discord.gg/dotnet)
- [GitHub Discussions](https://github.com/dotnet/runtime/discussions)

## Getting Help

If you encounter issues not covered in this guide:

1. Check the existing documentation in the `docs/` directory
2. Search for similar issues in the project's GitHub repository
3. Create a new issue in the GitHub repository with detailed information about the problem
4. Include your operating system, .NET SDK version, and steps to reproduce the issue