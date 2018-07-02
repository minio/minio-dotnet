# For maintainers only

## Responsibilities

Please go through this link [Maintainer Responsibility](https://gist.github.com/abperiasamy/f4d9b31d3186bbd26522)

### Setup your minio-dotnet Github Repository

Fork [minio-dotnet upstream](https://github.com/minio/minio-dotnet/fork) source repository to your own personal repository.
```sh
$ git clone https://github.com/$USER_ID/minio-dotnet
$ cd minio-dotnet
```

Minio .NET Library uses nuget for its dependency management https://nuget.org/

### Publishing a new package
The steps below assume that the package is being built on Ubuntu 16.04, with following dependencies met.
- Mono 4.4.2 or later
- dotnet-sdk-2.1

For installation instructions follow this [script](https://github.com/minio/minio-dotnet/blob/master/mono_install.sh)
#### Update package Version
- Update Minio/Minio.csproj with the next version in the <Version></Version> tag
 ```
 <Version>2.0.0</Version>
 ```

- Update Minio/MinioClient.cs with the next version in the `SystemUserAgent`.

#### Build
```sh
$ dotnet build -c Release

```
#### Verify
```sh
$ cd Minio.Functional.Tests
$ dotnet run
```
#### Setup your nuget and download all dependencies
```sh
$ nuget restore
```

#### Build a package
```sh
$ msbuild /t:pack ./Minio/Minio.csproj /p:Configuration=Release
... package built ...
```
#### Upload the package to nuget.org
Login to nuget.org(https://www.nuget.org) and find the API Key for Minio
```sh
$  export MINIO_API_KEY=??
$  nuget setApiKey $MINIO_API_KEY
$  nuget push .\Minio\artifacts\Minio.1.0.9.nupkg -Apikey $MINIO_API_KEY -src https://nuget.org
```
#### Verify package on nuget.org
Verify that latest versions of Minio packages are available on [Nuget](https://www.nuget.org/account/Packages).

#### Tag
Tag and sign your release commit, additionally this step requires you to have access to Minio's trusted private key.
$ git tag -s 2.0.0
$ git push
$ git push --tags

#### Announce
Announce new release by adding release notes at https://github.com/minio/minio-dotnet/releases from trusted@minio.io account. Release notes requires two sections highlights and changelog. Highlights is a bulleted list of salient features in this release and Changelog contains list of all commits since the last release.

To generate changelog

git log --no-color --pretty=format:'-%d %s (%cr) <%an>' <last_release_tag>..<latest_release_tag>