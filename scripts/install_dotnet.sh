#!/bin/bash
set -e

echo "Downloading ..."
if command -v wget &> /dev/null; then
    wget --quiet --show-progress --output-document=dotnet-install.sh "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh"
else
    curl --silent --progress-bar --output dotnet-install.sh "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh"
fi


chmod +x dotnet-install.sh
./dotnet-install.sh --version 5.0.209 --install-dir ./dotnet
rm ./dotnet-install.sh
