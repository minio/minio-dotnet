# For maintainers only

## Responsibilities

Please go through this link [Maintainer Responsibility](https://gist.github.com/abperiasamy/f4d9b31d3186bbd26522)

### Setup your minio-dotnet Github Repository

Fork [minio-dotnet upstream](https://github.com/minio/minio-dotnet/fork) source repository to your own personal repository.
```powershell
> git clone https://github.com/$USER_ID/minio-dotnet
> cd minio-dotnet
```

Minio .NET Library uses nuget for its dependency management https://nuget.org/

### Publishing new package

#### Setup your nuget and download all dependencies

```powershell
> nuget restore
```

#### Compile the project and build a package

```powershell
> .\packages\MSBuild.0.1.2\tools\Windows\MSBuild.exe /t:Rebuild /p:Configuration=Release
> nuget pack Minio/Minio.csproj
... package built ...
```

#### Go to nuget.org

Sign into nuget.org to [upload a package through browser](https://www.nuget.org/users/account/LogOn?ReturnUrl=%2Fpackages%2Fupload).