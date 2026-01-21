# ctfdeck-services

## Run tests 
dotnet test --collect:"XPlat Code Coverage" && reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html && start coveragereport/index.html