#!/usr/bin/env sh
set -eu

if [ "$#" -lt 1 ] || [ "$#" -gt 2 ]; then
    echo "Usage: $0 <elastic-ip-or-hostname> [output-directory]" >&2
    exit 1
fi

certificate_host="$1"
output_directory="${2:-secrets/nginx}"

if ! printf '%s' "$certificate_host" | grep -Eq '^[A-Za-z0-9.-]+$'; then
    echo "The certificate host contains unsupported characters." >&2
    exit 1
fi

if printf '%s' "$certificate_host" | grep -Eq '^([0-9]{1,3}\.){3}[0-9]{1,3}$'; then
    subject_alternative_name="IP:${certificate_host}"
else
    subject_alternative_name="DNS:${certificate_host}"
fi

mkdir -p "$output_directory"

openssl req \
    -x509 \
    -nodes \
    -newkey rsa:2048 \
    -sha256 \
    -days 365 \
    -keyout "${output_directory}/tls.key" \
    -out "${output_directory}/tls.crt" \
    -subj "/CN=${certificate_host}" \
    -addext "subjectAltName=${subject_alternative_name}" \
    -addext "keyUsage=critical,digitalSignature,keyEncipherment" \
    -addext "extendedKeyUsage=serverAuth"

chmod 600 "${output_directory}/tls.key"
chmod 644 "${output_directory}/tls.crt"

echo "Created ${output_directory}/tls.crt and ${output_directory}/tls.key for ${certificate_host}."
