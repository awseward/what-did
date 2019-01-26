#!/bin/sh

set -eu

export PATH="$HOME/.dotnet/tools:$PATH"

fake build --list
