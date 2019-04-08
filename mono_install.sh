#!/bin/bash
#
# MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#

UBUNTU_CODENAME="xenial"

# Download and install Mono and .NETCore for Ubuntu 16.04
main() {
	echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ $UBUNTU_CODENAME main" | sudo tee /etc/apt/sources.list.d/mono-official.list
	sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 417A0893
	sudo apt-get update
	sudo apt-get install -yq dotnet-dev-1.0.4
	sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
	echo "deb http://download.mono-project.com/repo/ubuntu $UBUNTU_CODENAME main" | sudo tee -a /etc/apt/sources.list.d/mono-official.list
	sudo apt-get update
	sudo apt-get install -yq  mono-complete 
	sudo apt-get install -yq ca-certificates-mono
	sudo apt-get install -yq mono-xsp4
}

main
