#!/bin/sh

set -eu

export PATH="$HOME/.dotnet/tools:$PATH"

dotnet tool install fake-cli -g

# fake

ls -lah
echo wut
