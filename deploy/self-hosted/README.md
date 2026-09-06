# Self-hosted deployment

This directory provides an OpenWish deployment workflow modeled after the
Eggcelent maintenance script. A normal run backs up PostgreSQL, prunes expired
backups, fast-forwards the source checkout, syncs the Compose file, pulls
images, deploys, checks HTTP health, and restores the previous application
image if health verification fails.

## Setup

Install Docker Engine with Compose v2, Git, curl, and tar. Keep a checkout owned
by your normal developer account. The scheduled job may run as root; Git and
Docker credentials are then taken from the checkout owner where possible.

Create a private environment file from the sample:

```bash
cp deploy/self-hosted/.env.example /opt/openwish/.env
chmod 600 /opt/openwish/.env
```

Set every required `OPENWISH_*` value. `OPENWISH_SOURCE_DIR` is the repository
checkout, `OPENWISH_DEPLOY_DIR` holds the active Compose file and error log, and
`OPENWISH_BACKUP_DIR` holds backup archives.

Run the complete maintenance sequence:

```bash
OPENWISH_ENV_FILE=/opt/openwish/.env \
  /home/your-user/src/OpenWish/deploy/self-hosted/maintain.sh
```

With no argument, `update` is implied. This makes the script suitable for a
root cron job or systemd timer. It uses `git pull --ff-only`; local source
changes or divergent history stop the deployment rather than being discarded.

## Configuration notes

The default listener is `127.0.0.1:5001`, intended for an HTTPS reverse proxy
on the same host. Set `OPENWISH_BIND_ADDRESS=0.0.0.0` only when direct network
access is intentional. Configure the exact trusted proxy address or network
when forwarded headers are used.

The default health URL checks the root page and accepts HTTP 2xx or 3xx. Set
`HEALTH_URL` to the URL reachable from the host if the listener or proxy setup
differs.

Pin `OPENWISH_IMAGE` to a published release tag for controlled upgrades.
Database backups are compressed archives containing a custom-format PostgreSQL
dump. Copy them to separate storage; local backups do not protect against host
failure.

## Other commands

```bash
./maintain.sh backup
./maintain.sh deploy
./maintain.sh pull
./maintain.sh status
./maintain.sh logs
./maintain.sh logs web
./maintain.sh stop
```

Each command uses `.env` beside the script unless `OPENWISH_ENV_FILE` points to
another file.

Restore a backup:

```bash
./maintain.sh restore /opt/backups/openwish/openwish-backup-TIMESTAMP.tar.gz
```

Restore requires typing `RESTORE`, creates another backup first, and stops the
web service while replacing the database. Test recovery periodically.

Do not run `docker compose down --volumes` unless permanent database deletion
is intended. Automated rollback restores the previous application image and
Compose file; it does not reverse database migrations. The pre-deployment
backup is the recovery point if a release requires database restoration.
