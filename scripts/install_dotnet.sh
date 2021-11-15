#!/bin/bash
set -e

curl --silent --progress-bar --output dotnet-install.sh "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh"
./dotnet-install.sh --version 5.0.209 --install-dir ./dotnet
rm ./dotnet-install.sh
