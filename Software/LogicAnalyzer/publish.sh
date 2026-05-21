#!/bin/bash

path_of_self="$(realpath -- "$(dirname -- "${0}")")"
cd "${path_of_self}"

DOTNET_ROOT="${DOTNET_ROOT-$HOME/.dotnet}"

case ":${PATH}:" in
    *":${DOTNET_ROOT}:"*)
        ;;
    *)
        PATH="$DOTNET_ROOT:$PATH"
        ;;
esac

echo "${PATH}"

dotnet restore LogicAnalyzer.sln
dotnet build LogicAnalyzer.sln -c Release
dotnet publish LogicAnalyzer.sln -c Release -p:PublishProfile=Linux

rm -rf Release
mkdir -p Release

for release in */bin/Release/*/linux-x64; do
    cp -rf "${release}"/* Release
done

cp -rf ../decoders/ Release
