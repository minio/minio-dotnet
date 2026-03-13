# minio-dotnet Agent Guidelines

## Build & Test Commands

### Project Structure
- **Main Library**: `Minio/Minio.csproj` (net8.0, net9.0, net10.0)
- **Unit Tests**: `Minio.UnitTests/Minio.UnitTests.csproj` (xUnit, net8.0+)
- **Integration Tests**: `Minio.IntegrationTests/Minio.IntegrationTests.csproj` (xUnit with Testcontainers)
- **Multipart Upload Tests**: `Minio.IntegrationTests/Tests/MultipartUploadTests.cs` (10 comprehensive tests)
- **Checksum Verification**: `Minio.Helpers.ChecksumVerifyingStream` + `ChecksumVerificationException` classes

### Build & Restore
```bash
dotnet restore
dotnet build
dotnet build Minio/Minio.csproj
dotnet build Minio.UnitTests/Minio.UnitTests.csproj
dotnet build --configuration Release
```

### Running Tests

#### Unit Tests (xUnit)
```bash
dotnet test Minio.UnitTests/Minio.UnitTests.csproj
dotnet test Minio.UnitTests/Minio.UnitTests.csproj --filter "FullyQualifiedName~MinioClientBuilderTests.EnsureMinioClient"
dotnet test Minio.UnitTests/Minio.UnitTests.csproj --filter "FullyQualifiedName~MinioClientBuilderTests"
dotnet test --verbosity detailed
```

#### Integration Tests
```bash
dotnet test Minio.IntegrationTests/Minio.IntegrationTests.csproj
dotnet test Minio.IntegrationTests/Minio.IntegrationTests.csproj --filter "FullyQualifiedName~BucketTests"
```

#### Examples
```bash
dotnet run --project Minio.Examples.Simple/Minio.Examples.Simple.csproj
dotnet run --project Minio.Examples.Host/Minio.Examples.Host.csproj
```

### Code Analysis
```bash
dotnet build /p:EnableNETAnalyzers=true /p:AnalysisLevel=latest
```

**Code Quality Standards:**
- TreatWarningsAsErrors enabled - all code must compile without warnings
- NullableReferenceTypes enabled - all nullable scenarios must be handled
- XML comments required for all public APIs
- Async: Use `ConfigureAwait(false)` in library code, `ConfigureAwait(true)` in tests
- Null safety: Use `ArgumentNullException.ThrowIfNull()`, `ArgumentException.ThrowIfNullOrEmpty()`
- Resource management: Properly dispose streams via `await using` pattern
- Naming: PascalCase for public members, camelCase for private fields

## Code Style Guidelines

### C# Coding Conventions
- File-scoped namespaces, using directives outside namespace blocks
- PascalCase for classes/structs/interfaces/enums/methods/properties (interfaces start with `I`)
- camelCase for private fields (no underscore prefix)
- 4-space indentation, LF line endings, braces on new line
- Expression-bodied members for simple single-line methods/properties
- Prefer auto-properties over backing fields
- Use `var` for type-obvious scenarios
- Always specify accessibility modifiers
- Use `internal` for implementation details, `public` for public API
- Use `readonly` for immutable fields, `sealed` for non-inheritable classes

### Async/Await Patterns
- Use `async Task` for async void methods (events)
- Use `async Task<T>` for methods returning a value
- Always use `ConfigureAwait(false)` in library code
- Use `ValueTask` for performance-critical paths

### Error Handling
- Throw specific exception types (`MinioException`, `InvalidOperationException`)
- Use descriptive exception messages
- Validate input parameters in public API methods
- Parse server responses and convert to appropriate exceptions

### Naming Specifics
- Use fluent builder pattern: `With[PropertyName]()` methods return `this`
- Use `Async` suffix for async methods
- Use `Task` return type for async methods

### Using Directives
- Sort using directives alphabetically within groups
- Group using directives (system, then external packages, then internal namespaces)
- Remove unused using directives
- Use aliased using for generic types

### XML Documentation
- Document all public API members
- Use `<summary>`, `<param>`, `<returns>`, `<exception>`, `<example>` tags
- Include usage examples for complex methods
- Use `<see>` and `<paramref>` for cross-references

## Testing Guidelines

### Unit Tests (xUnit)
- Use xUnit framework (`[Fact]`, `[Theory]` attributes)
- Test one behavior per method
- Use descriptive test method names (`MethodName_Scenario_ExpectedBehavior`)
- Use `Assert.Throws<T>` for exception testing
- Use `[InlineData]` for parameterized tests
- Mock dependencies using Moq or create test doubles manually

### Test Organization
- Unit tests in `Minio.UnitTests/` directory
- Integration tests in `Minio.IntegrationTests/` directory
- Base test classes: `MinioTest` (uses Testcontainers)
- Clean up resources in `DisposeAsync()` or `[Fact]` cleanup code

### Integration Tests
- Use `MinioTest` base class for integration tests
- Tests use Testcontainers (MinIO, NATS, Keycloak)
- Environment variables: `SERVER_ENDPOINT`, `ACCESS_KEY`, `SECRET_KEY` (when running externally)

## Project-Specific Patterns

### Builder Pattern
```csharp
var client = new MinioClientBuilder("https://minio.example.com")
    .WithStaticCredentials("accessKey", "secretKey")
    .Build();
```

### Credentials Providers
Multiple credential providers via `ICredentialsProvider`:
- `StaticCredentialsProvider` - Static access key/secret
- `EnvironmentCredentialsProvider` - AWS environment variables
- `AccessTokenProvider` - Token-based authentication
- Keycloak integration via `KeycloakAccessTokenProvider`

### HTTP Client
- Uses `HttpClient` for all HTTP operations
- Custom `HttpClientFactory` for client management
- Request signing via `V4RequestAuthenticator` (AWS Signature Version 4)

### Configuration
#### Directory.Build.props
- Target frameworks: net8.0, net9.0, net10.0
- LangVersion: latest, implicit usings enabled, nullable enabled
- AnalysisMode: AllEnabledByDefault, SourceLink enabled
- TreatWarningsAsErrors: true, GenerateDocumentationFile: true

#### .editorconfig
- Comprehensive code style rules configured
- CA rules suppressed where not applicable (see .editorconfig comments for details)

### Server-Side Encryption (SSE)

The SDK supports three SSE types matching MinIO/Rust SDK:

#### SSE-S3 (S3-managed keys)
```csharp
var client = new MinioClientBuilder("https://minio.example.com")
    .WithStaticCredentials("accessKey", "secretKey")
    .Build();

var putOptions = new PutObjectOptions
{
    ServerSideEncryption = new SseS3Config()
};

await client.PutObjectAsync("mybucket", "myobject", stream, putOptions, null, cancellationToken);
```

#### SSE-KMS (AWS KMS-managed keys)
```csharp
var putOptions = new PutObjectOptions
{
    ServerSideEncryption = new SseKmsConfig() // Uses default KMS key
};

// Or with specific key ID
var putOptions = new PutObjectOptions
{
    ServerSideEncryption = new SseKmsConfig("my-kms-key-id")
};
```

#### SSE-C (Customer-provided keys)
Not yet implemented (requires customer key management).

#### Get Object with SSE
```csharp
var getOptions = new GetObjectOptions
{
    ServerSideEncryption = new SseS3Config()
};

await using var stream = await client.GetObjectAsync("mybucket", "myobject", getOptions, cancellationToken);
// Checksum verification is automatic when checksums are present in ObjectInfo
```

### Checksum Verification

Automatic checksum verification during download:
- Supports CRC32, CRC32C, CRC64NVME, SHA1, SHA256
- Automatically wraps streams via `ChecksumVerifyingStream`
- Throws `ChecksumVerificationException` on mismatch

**Note**: For put operations, specify checksum algorithm:
```csharp
var putOptions = new PutObjectOptions
{
    ChecksumAlgorithm = ChecksumAlgorithm.Crc32c
};
```

**Note**: CRC64NVME (matching Rust SDK) is recommended for new applications as it provides the most accurate checksums.

For multipart uploads, set checksums for individual parts:
```csharp
var part1 = new PartInfo 
{ 
    PartNumber = 1, 
    Etag = "part1-etag",
    ChecksumAlgorithm = ChecksumAlgorithm.Crc64nvme,
    Checksum = crc64nvmeBytes
};
```

### Error Responses
- Custom exception hierarchy starting with `MinioException`
- `MinioHttpException` for HTTP-specific errors

## CI/CD Notes

- SourceLink enabled for debugging
- Documentation and symbol packages generated on Release builds
- GitHub Actions for CI/CD
- NuGet package: https://www.nuget.org/packages/Minio/

## Build Status

- **Unit Tests**: 213 tests pass across net8.0, net9.0, net10.0
- **Integration Tests**: Uses Testcontainers for MinIO (requires Docker)
- **Build**: 0 errors, 0 warnings with TreatWarningsAsErrors enabled
