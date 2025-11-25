#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 2 || -z "${1}" || -z "${2}" ]]; then
  echo "Usage: $0 <common-name> <pfx-password>"
  echo "Example: $0 origin.openwish.test Str0ngP@ss!"
  exit 1
fi

CN=$1
PASSWORD=$2
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CERT_DIR="${SCRIPT_DIR}/certs"

echo "Ensuring cert directory exists at ${CERT_DIR}..."
mkdir -p "${CERT_DIR}"

echo "Creating TLS key and certificate for CN=${CN}..."
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout "${CERT_DIR}/tls.key" \
  -out "${CERT_DIR}/tls.crt" \
  -subj "/CN=${CN}"

echo "Packaging password-protected PFX..."
openssl pkcs12 -export \
  -out "${CERT_DIR}/tls.pfx" \
  -inkey "${CERT_DIR}/tls.key" \
  -in "${CERT_DIR}/tls.crt" \
  -password "pass:${PASSWORD}"

echo "Done!"