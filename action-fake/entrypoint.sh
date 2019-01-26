#!/bin/sh

set -eu

export PATH="/root/.dotnet/tools:$PATH"
export FAKE_DETAILED_ERRORS=true

mono --version

fake build -t "$1"
