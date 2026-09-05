# Self-Signed Certificate Guide

## Overview
When Cloudflare terminates TLS in front of OpenWish you can optionally mount a locally issued certificate inside the container. Set `TLS__CertificatePath` to the mounted PFX bundle (typically `/certs/tls.pfx`) to expose HTTPS on port `8443`. Without that setting, the site listens on HTTP only so a trusted reverse proxy can terminate TLS.

## Generate a Development Certificate with `dotnet dev-certs`
1. Clean any previous developer certificates (optional):
   ```bash
dotnet dev-certs https --clean
```
2. Export a new developer certificate to PFX format:
   ```bash
dotnet dev-certs https -ep certs/tls.pfx -p "<strong-password>"
```
3. (Optional) Trust the certificate locally so browsers accept it:
   ```bash
dotnet dev-certs https --trust
```

## Generate a Cross-Platform Certificate with OpenSSL
1. Create a private key and self-signed certificate valid for 365 days:
   ```bash
mkdir -p certs
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout certs/tls.key -out certs/tls.crt \
  -subj "/CN=openwish.test"
```
2. Package the key and certificate into a password-protected PFX:
   ```bash
openssl pkcs12 -export -out certs/tls.pfx \
  -inkey certs/tls.key -in certs/tls.crt \
  -password pass:<strong-password>
```

## Run the Container with the Certificate
```bash
docker run --rm \
  -p 8080:8080 -p 8443:8443 \
  -v "$(pwd)/certs:/certs:ro" \
  -e TLS__CertificatePath=/certs/tls.pfx \
  -e TLS__CertificatePassword=<strong-password> \
  ghcr.io/<your-org>/openwish:latest
```
- Mounts the `certs` directory read-only at `/certs`.
- Exposes HTTP on `8080` and HTTPS on `8443`.
- The password is optional when the PFX has no password.

## Application Configuration Reference
| Setting | Description | Default |
|---------|-------------|---------|
| `TLS__CertificatePath` | Absolute path to the mounted PFX bundle. | _unset_ |
| `TLS__CertificatePassword` | Password for the PFX file, if one was set. | _empty_ |
| `Tls:HttpPort` | Override the HTTP listener port. | `8080` in container, else `5000` |
| `Tls:HttpsPort` | Override the HTTPS listener port. | `8443` in container, else `5001` |
| `ForwardedHeaders:KnownProxies` | Proxy IP addresses trusted to set forwarded headers. | _none_ |
| `ForwardedHeaders:KnownNetworks` | Proxy CIDR ranges trusted to set forwarded headers. | _none_ |

Forwarded headers are disabled until at least one valid trusted proxy or network is configured. Add the relevant lists to your `appsettings.Production.json` or environment variables:
```json
{
  "ForwardedHeaders": {
    "KnownProxies": ["103.21.244.1"],
    "KnownNetworks": ["103.21.244.0/22"]
  }
}
```

## Verify the Certificate
After the container starts, test the HTTPS endpoint:
```bash
curl -k https://localhost:8443/
```
Use `-k` (or `--insecure`) only with self-signed certificates. Replace `localhost` with the hostname Cloudflare uses when testing end-to-end TLS.
