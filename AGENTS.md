# minio-dotnet Agent Guidelines

## Build & Test Commands

### Project Structure
- **Main Library**: `Minio/Minio.csproj` (net6.0, net7.0, netstandard2.0)
- **Unit Tests**: `Minio.Tests/Minio.Tests.csproj` (MSTest, net6.0)
- **Functional Tests**: `Minio.Functional.Tests/Minio.Functional.Tests.csproj` (console app, net6.0, net7.0)
- **Examples**: `Minio.Examples/Minio.Examples.csproj`
- **Simple Test**: `SimpleTest/SimpleTest.csproj`

### Build & Restore
```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Build specific project
dotnet build Minio/Minio.csproj
dotnet build Minio.Tests/Minio.Tests.csproj

# Build in Release mode
dotnet build --configuration Release
```

### Running Tests

#### Unit Tests (MSTest)
```bash
# Run all unit tests
dotnet test Minio.Tests/Minio.Tests.csproj

# Run a specific test method
dotnet test Minio.Tests/Minio.Tests.csproj --filter "FullyQualifiedName~UtilsTest.TestValidPartSize1"

# Run tests in a specific test class
dotnet test Minio.Tests/Minio.Tests.csproj --filter "FullyQualifiedName~UtilsTest"

# Run tests with verbose output
dotnet test Minio.Tests/Minio.Tests.csproj --verbosity detailed
```

#### Functional Tests
```bash
# Run functional tests (requires MinIO server)
cd Minio.Functional.Tests
dotnet run

# With custom endpoint
SERVER_ENDPOINT="myserver:9000" ACCESS_KEY="mykey" SECRET_KEY="mysecret" dotnet run

# Run core tests only
MINT_MODE=core dotnet run
```

#### Simple Test
```bash
dotnet run --project SimpleTest/SimpleTest.csproj
```

### Linting & Code Analysis
```bash
# Run Roslyn analyzers
dotnet build /p:EnableNETAnalyzers=true /p:AnalysisLevel=latest

# Check for warnings as errors (configured in Directory.Build.props)
dotnet build
```

## Code Style Guidelines

### C# Coding Conventions

#### Namespace Declarations
- Use file-scoped namespaces (`namespace Minio.DataModel;` not block-scoped)
- using directives place **outside** namespace blocks

#### Naming Conventions
- **Classes/Structs/Interfaces/Enums**: PascalCase (`MinioClient`, `BucketArgs<T>`, `IMinioClient`)
- **Interfaces**: Start with `I` (`IMinioClient`, `IRequestLogger`)
- **Methods**: PascalCase (`WithEndpoint`, `Build`, `ValidateBucketName`)
- **Properties**: PascalCase (`BucketName`, `Endpoint`, `RequestTimeout`)
- **Private fields**: camelCase (no underscore prefix: `private bool disposedValue`)
- **Constants**: PascalCase (`DefaultEndpoint`, `MaximumStreamObjectSize`)

#### Formatting
- **Indentation**: 4 spaces
- **Line endings**: LF (Unix-style)
- **Braces**: Always on new line (all methods, classes, properties)
- **Expression-bodied members**: Use for simple methods/properties
- **Auto-properties**: Prefer auto-properties over backing fields
- **var keyword**: Use for type-obvious scenarios (`var client = new MinioClient()`)

#### Accessibility
- Always specify accessibility modifiers (`public`, `private`, `internal`)
- `internal` for implementation details, `public` for public API
- Use `readonly` for fields that don't change

### Async/Await_patterns
- Use `async Task` for void-returning async methods (events)
- Use `async Task<T>` for methods returning a value
- Always use `ConfigureAwait(false)` in library code
- Use `ValueTask` for performance-critical paths when appropriate

### Error Handling
- Throw specific exception types (`MinioException`, `InvalidBucketNameException`)
- Use exception messages that clearly describe the issue
- Validate input parameters in public API methods
- Parse server responses and convert to appropriate exceptions

### Naming Specifics
- Use `Args` suffix for request argument classes (`MakeBucketArgs`, `BucketExistsArgs`)
- Use fluent builder pattern: `With[PropertyName]()` methods return `this`
- Use `Async` suffix for async methods when同步 version exists
- Use `Task` return type for async methods

### Using Directives
- Sort system directives first (`using System.*`)
- Group using directives (system, then external packages, then internal namespaces)
- Remove unused using directives

### XML Documentation
- Document all public API members
- Use `<summary>`, `<param>`, `<returns>`, `<exception>` tags
- Include example usage for complex methods

## Testing Guidelines

### Unit Tests
- Use MSTest framework (`[TestClass]`, `[TestMethod]`)
- Test one behavior per method
- Use descriptive test method names (`TestMethodName_Scenario_ExpectedBehavior`)
- Verify expected exceptions with `[ExpectedException]` attribute or `Assert.ThrowsException`
- Use `TestHelper.GetRandomName()` for generating test names

### Test Organization
- Tests located in `Minio.Tests/` directory
- Mock external dependencies using Moq
- Test both success and failure scenarios
- Avoid network calls in unit tests

### Functional Tests
- Located in `Minio.Functional.Tests/`
- Require a running MinIO server
- Environment variables: `SERVER_ENDPOINT`, `ACCESS_KEY`, `SECRET_KEY`, `ENABLE_HTTPS`
- Use `FunctionalTest.GetRandomName()` for bucket/object names
- Clean up resources after tests

## Project-Specific Patterns

### Builder Pattern
The client uses a fluent builder pattern:
```csharp
var client = new MinioClient()
    .WithEndpoint("play.min.io")
    .WithCredentials("access", "secret")
    .WithSSL()
    .Build();
```

### Argument Classes
All operations use argument classes:
```csharp
var args = new MakeBucketArgs().WithBucket("mybucket");
await client.MakeBucketAsync(args);
```

### Credential Providers
Supports multiple credential providers (`IClientProvider` interface):
- AWS Environment Provider
- Assume Role Provider
- Certificate Identity Provider
- Web Identity Provider
- Chained Provider

## Configuration

### Directory.Build.props
- Configures analyzers, warnings as errors, code style enforcement
- Auto-includes SourceLink for debugging
- Generates documentation and symbol packages on Release builds
- `<ImplicitUsings>enable</ImplicitUsings>` and `<Nullable>enable</Nullable>` commented out

### .editorconfig
- Configured to remove noise and increase conciseness
- Deviations from VS defaults documented in comments
- Disables underscore prefix on private fields
- Uses file-scoped namespace declarations
- LF line endings
