# Testing

The project uses **xUnit** for unit and integration tests.

## Test structure

Tests are organized by component type:

-   **ControllersTests**: API endpoint tests (unit tests with mocks)
-   **ServicesTests**: Business logic tests (unit tests)
-   **IntegrationTests**: End-to-end tests with `WebApplicationFactory`
-   **ModelsTests**: Model validation tests
-   **UtilitiesTests**: Utility and configuration tests

## Test framework

-   **xUnit** 2.7.0
-   **Moq** for mocking dependencies
-   **Microsoft.AspNetCore.Mvc.Testing** for integration tests
-   **Coverlet** for code coverage

## Running tests

```bash
dotnet test
```

With coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Note:** Only the `/test` directory is used to generate coverage reports showing in terminal.

Coverage reports are generated in `Tests/TestResults/` (Cobertura, JSON, LCOV formats).

## Test configuration

Environment variables for tests are defined in `testsettings.runsettings`:

## Coverage

The project maintains **100% code coverage** (line, branch, and method coverage).

Coverage excludes:

-   Test assemblies (`[*.Tests]*`, `[*.Test]*`)
-   Generated code (`GeneratedCodeAttribute`, `CompilerGeneratedAttribute`)
-   Obsolete code (`ObsoleteAttribute`)
