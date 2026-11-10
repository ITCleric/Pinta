#!/bin/sh
set -eu
project_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$project_root"
if [ -x "$project_root/.dotnet/dotnet" ]; then
  export DOTNET_ROOT="$project_root/.dotnet"
  dotnet_bin="$DOTNET_ROOT/dotnet"
else
  dotnet_bin=dotnet
fi
export DYLD_LIBRARY_PATH="$(brew --prefix)/lib${DYLD_LIBRARY_PATH:+:$DYLD_LIBRARY_PATH}"
"$dotnet_bin" build Pinta/Pinta.csproj --nologo
exec "$dotnet_bin" "$project_root/build/bin/Pinta.dll" "$@"
