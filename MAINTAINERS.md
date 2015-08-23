# For maintainers only

### Setup your minio-dotnet Github Repository

Fork [minio-dotnet upstream](https://github.com/minio/minio-dotnet/fork) source repository to your own personal repository.
```bash
$ git clone https://github.com/$USER_ID/minio-dotnet
$ cd minio-dotnet
```

Minio .NET Library uses nuget for its dependency management https://nuget.org/

### Publishing new package

#### Setup your nuget and download all dependencies

```bash
$ wget https://www.nuget.org/nuget.exe
$ mono nuget.exe restore
```

#### Compile the project and build a package

```bash
$ xbuild
$ mono nuget.exe pack Minio/Minio.csproj
... package built ...
```

#### Go to nuget.org

Sign into nuget.org to [upload a package through browser](https://www.nuget.org/users/account/LogOn?ReturnUrl=%2Fpackages%2Fupload).