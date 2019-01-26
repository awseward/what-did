#!/bin/sh

set -eu

export PATH="/root/.dotnet/tools:$PATH"
export FAKE_DETAILED_ERRORS=true

fake build -t "$1"
