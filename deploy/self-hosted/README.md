# Self-hosted deployment

This directory is a portable OpenWish deployment for a single Docker host. It
runs the published OpenWish image with PostgreSQL, keeps the database in a
named Docker volume, and stores logical backups on the host.

## Initial deployment

1. Install Docker Engine with the Docker Compose v2 plugin.
2. Copy this directory to the deployment host.
3. Create the private configuration and restrict its permissions:

   ```bash
   cp .env.example .env
   chmod 600 .env
   ```

4. Edit `.env`. At minimum, replace `POSTGRES_PASSWORD`, set
   `OPENWISH_BASE_URI`, and verify the image tag, time zone, listener, and
   trusted reverse proxy.
5. Start OpenWish:

   ```bash
   ./maintain.sh deploy
   ```

The default listener is `127.0.0.1:5001`, intended for a reverse proxy on the
same host. Set `OPENWISH_BIND_ADDRESS=0.0.0.0` only when direct network access
is intentional. The reverse proxy must provide HTTPS in the default setup.

## Routine maintenance

Run a database backup without changing the deployment:

```bash
./maintain.sh backup
```

Pull the configured images without restarting:

```bash
./maintain.sh pull
```

Perform the normal update sequence (backup, pull, and recreate):

```bash
./maintain.sh update
```

Use a versioned `OPENWISH_IMAGE` tag in `.env` for controlled upgrades. Review
the release notes before changing the tag. Backups are custom-format PostgreSQL
dumps in `BACKUP_DIR`; copy them to separate storage as part of the host backup
policy. Files older than `BACKUP_RETENTION_DAYS` are removed after a successful
backup.

## Recovery and diagnostics

List services or follow logs:

```bash
./maintain.sh status
./maintain.sh logs
./maintain.sh logs web
```

Restore a backup:

```bash
./maintain.sh restore ./backups/openwish-YYYYMMDDTHHMMSSZ.dump
```

Restore requires typing `RESTORE`, creates another backup first, and stops the
web service while replacing the database. Test recovery periodically and
retain off-host copies; a backup stored only beside the deployment does not
protect against host failure.

To stop containers without deleting the database volume:

```bash
./maintain.sh stop
```

Do not run `docker compose down --volumes` unless permanent database deletion
is intended.
