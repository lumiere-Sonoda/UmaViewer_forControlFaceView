#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORKSPACE_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

SRC_FILE="${SCRIPT_DIR}/StandaloneFileBrowser.m"
TARGET_FILE="${WORKSPACE_ROOT}/Assets/Plugins/StandaloneFileBrowser/Plugins/StandaloneFileBrowser.bundle/Contents/MacOS/StandaloneFileBrowser"

mkdir -p "$(dirname "${TARGET_FILE}")"

xcrun clang \
  -fobjc-arc \
  -bundle \
  -framework Cocoa \
  -mmacosx-version-min=10.13 \
  -arch x86_64 \
  -arch arm64 \
  "${SRC_FILE}" \
  -o "${TARGET_FILE}"

chmod +x "${TARGET_FILE}"
file "${TARGET_FILE}"
