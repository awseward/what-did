#!/bin/sh

set -eu

export PATH="/root/.dotnet/tools:$PATH"

echo $PATH

fake build --list
