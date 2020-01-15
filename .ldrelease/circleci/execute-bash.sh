#!/bin/bash

set -ue

# Performs a delegated release step in a CircleCI Ubuntu container. This mechanism is described
# in scripts/circleci/README.md. All of the necessary environment variables should already be
# in the generated CircleCI configuration.

STEP="$1"
cd "$(dirname "$0")"
CIRCLECI_DIR=$(pwd)
cd ..
LDRELEASE_DIR=$(pwd)
cd ..  # now we are in the project directory

echo
echo -n "[${STEP}] "
SCRIPT_NAME=${STEP}.sh
PROJECT_HOST_SPECIFIC_SCRIPT_PATH="${LDRELEASE_DIR}/${LD_RELEASE_CIRCLECI_TYPE}-${SCRIPT_NAME}"
PROJECT_SCRIPT_PATH="${LDRELEASE_DIR}/${SCRIPT_NAME}"
TEMPLATE_SCRIPT_PATH="${CIRCLECI_DIR}/template/${SCRIPT_NAME}"
found=n
for path in "${PROJECT_HOST_SPECIFIC_SCRIPT_PATH}" "${PROJECT_SCRIPT_PATH}" "${TEMPLATE_SCRIPT_PATH}"; do
  if [ -f "${path}" ]; then
    echo "executing ${path}"
    "${path}"
    found=y
    break
  fi
done
if [ "${found}" == "n" ]; then
  echo "script ${SCRIPT_NAME} is undefined, skipping"
fi
