# CLAUDE.md
This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
MinIO .NET SDK - A modern C# client library for MinIO S3-compatible object storage. Multi-targeted for .NET 8.0 and later.

## Build Commands
Make sure you have installed a recent .NET Core SDK.
```bash
# Build
dotnet build

# Run unit tests
dotnet test Minio.UnitTests

# Run a single unit test
dotnet test Minio.UnitTests --filter "FullyQualifiedName~TestMethodName"

# Run integration tests (requires Docker for Testcontainers)
dotnet test Minio.IntegrationTests
```

## Project Structure
```
Minio/                    - Core SDK library
Minio.UnitTests/          - Unit tests (xUnit)
Minio.IntegrationTests/   - Integration tests using Testcontainers (MinIO, Keycloak, NATS)
Minio.Examples.Simple/    - Minimal console example
Minio.Examples.Host/      - DI-based example with IHost
```

## Architecture
### Client Creation
Two patterns for creating the MinIO client:

**Direct builder:**
```csharp
var client = new MinioClientBuilder("https://minio.example.com")
    .WithStaticCredentials("accessKey", "secretKey")
    .Build();
```

**Dependency Injection:**
```csharp
services
    .AddMinio("http://localhost:9000")
    .WithStaticCredentials("minioadmin", "minioadmin");
```

### Key Components
- `IMinioClient` - Main interface with bucket and object operations
- `MinioClientBuilder` - Fluent builder for direct client creation
- `IMinioBuilder` - DI configuration builder (via `AddMinio` extension)
- `ICredentialsProvider` - Interface for credentials (static, environment, or STS/OIDC)
- `V4RequestAuthenticator` - AWS Signature Version 4 request signing

### Credential Providers
- `EnvironmentCredentialsProvider` - Uses `MINIO_ROOT_USER` and `MINIO_ROOT_PASSWORD` env vars
- `StaticCredentialsProvider` - Hardcoded credentials
- `WebIdentityCredentialsProvider` - STS/OIDC token-based authentication

### Request Handling
- Polly retry policy with exponential backoff (5 retries, 250ms median first delay)
- Retryable status codes: 408, 423, 429, 500, 502, 503, 504
- All async operations support `CancellationToken`
- Streaming operations use `IAsyncEnumerable<T>` for collections and `IObservable<T>` for notifications

## Code Conventions
- Nullable reference types enabled throughout
- `InternalsVisibleTo` allows unit tests to access internal classes
- XML documentation generation is enabled
- Uses `Microsoft.Extensions.*` patterns for DI, logging, and options
- Public API is documented with XML comments
- All MinIO SDK and example code should be compatible with .NET 6.0 and later.
- All unit and integration tests should be compatible with .NET 8.0 and later.
