# MinIO .NET SDK

A modern C# client library for [MinIO](https://min.io) and S3-compatible object storage services.

## Requirements

- .NET 8.0, 9.0, or 10.0
- A MinIO or S3-compatible server

## Quick Start

### Direct usage

```csharp
var client = new MinioClientBuilder("https://minio.example.com")
    .WithStaticCredentials("accessKey", "secretKey")
    .Build();
```

### Dependency Injection

```csharp
services
    .AddMinio("http://localhost:9000")
    .WithStaticCredentials("minioadmin", "minioadmin");
```

## Examples

Two example projects are included:

- `Minio.Examples.Simple/` — minimal console app using the direct builder
- `Minio.Examples.Host/` — DI-based example using `IHost`, demonstrating bucket creation, object upload/download, listing, and bucket notifications

To run an example, start a local MinIO instance first:

```bash
docker run --rm -p 9000:9000 quay.io/minio/minio:latest server /data
```

Then:

```bash
dotnet run --project Minio.Examples.Simple
# or
dotnet run --project Minio.Examples.Host
```
