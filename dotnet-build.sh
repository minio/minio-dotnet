#!/bin/bash

set -x
set -e
for rid in $(echo win-x64 win-arm64 linux-x64 linux-arm64); do
    echo "restore & build for Release"
    dotnet build -r $rid --configuration Release ./Minio/Minio.csproj

    echo "Strong name sign assembly"
    if [ -f ./Minio/bin/Release/netstandard2.0/${rid}/Minio.dll ]; then
	echo "found"
	sn -R ./Minio/bin/Release/netstandard2.0/${rid}/Minio.dll ./Minio.snk
    fi

    if [ -f ./Minio/bin/Release/netstandard2.1/${rid}/Minio.dll ]; then
	sn -R ./Minio/bin/Release/netstandard2.1/${rid}/Minio.dll ./Minio.snk
    fi
done

dotnet pack ./Minio/Minio.csproj --no-build --configuration Release --output ./artifacts
