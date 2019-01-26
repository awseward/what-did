#!/bin/sh

set -eu

export PATH="/root/.dotnet/tools:$PATH"
export FAKE_DETAILED_ERRORS=true
export HEROKU_REMOTE=staging

mono --version

fake build -t "$1"
