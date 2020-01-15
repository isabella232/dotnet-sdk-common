#!/bin/bash

set -ue

# Performs a delegated release step in a CircleCI Ubuntu container. This mechanism is described
# in scripts/circleci/README.md.

mkdir -p artifacts
"$(dirname "$0")/../execute-bash.sh" "$1"
