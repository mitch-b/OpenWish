# OpenWish Self-Hosting Guide

This guide provides comprehensive instructions for operators deploying OpenWish in self-hosted environments. OpenWish is designed for self-hosting confidence—you should understand setup, upgrades, backups, and configuration with minimal OpenWish-specific knowledge.

## Prerequisites

### System Requirements

- **Docker** 20.10+ and **Docker Compose** 2.0+ (for Docker deployment)
- **PostgreSQL** 13+ (as the persistent data store)
- **Minimum Hardware**: 512 MB RAM, 1 vCPU, 5 GB storage
- **Recommended Hardware**: 2+ GB RAM, 2+ vCPU, 20 GB storage for typical group usage
- **Network**: Stable internet connection; OpenWish does not require outbound internet except for optional external features (Google sign-in, product metadata import)

### Supported Operating Systems

- Linux (Ubuntu 20.04+, Debian 11+, CentOS 8+)
- macOS (10.14+, Intel or Apple Silicon)
- Windows (Windows Server 2019+, or Windows 10+ with WSL 2)

## Quick Start with Docker Compose

The fastest way to run OpenWish is with Docker Compose, which provisions both PostgreSQL and the OpenWish application.

### Basic Deployment

1. **Create a directory for OpenWish**:
   ```bash
   mkdir -p ~/openwish && cd ~/openwish
   ```

2. **Create `docker-compose.yml`**:
   ```yaml
   version: '3.8'
   
   services:
     database:
       image: postgres:18
       container_name: openwish-postgres
       environment:
         POSTGRES_USER: openwish
         POSTGRES_PASSWORD: "YourStrong!Passw0rd"  # Change this
         POSTGRES_DB: OpenWish
       volumes:
         - openwish-data:/var/lib/postgresql/data
       ports:
         - "5432:5432"
       healthcheck:
         test: ["CMD-SHELL", "pg_isready -U openwish"]
         interval: 10s
         timeout: 5s
         retries: 5
       restart: unless-stopped
   
     web:
       image: ghcr.io/mitch-b/openwish-web:latest
       container_name: openwish-web
       environment:
         TZ: America/Chicago  # Change to your timezone
         ConnectionStrings__OpenWish: "Server=database;Port=5432;Database=OpenWish;User Id=openwish;Password=YourStrong!Passw0rd"
         OpenWishSettings__OwnDatabaseUpgrades: "true"
       ports:
         - "5001:8080"
       depends_on:
         database:
           condition: service_healthy
       restart: unless-stopped
   
   volumes:
     openwish-data:
   ```

3. **Start the services**:
   ```bash
   docker-compose up -d
   ```

4. **Access OpenWish** at `http://localhost:5001`

5. **Create your first account** by clicking "Register" on the login page.

### Upgrading to a Newer Version

1. **Check available versions** at [OpenWish releases](https://github.com/mitch-b/OpenWish/pkgs/container/openwish-web/versions)

2. **Update the image tag** in `docker-compose.yml`:
   ```yaml
   web:
     image: ghcr.io/mitch-b/openwish-web:202610  # Use YYYYMM tag
   ```

3. **Stop and update**:
   ```bash
   docker-compose down
   docker-compose pull
   docker-compose up -d
   ```

4. **Verify the upgrade** by checking the version in the OpenWish UI (footer or settings)

## Environment Configuration

All OpenWish configuration uses environment variables. This section documents the available settings.

### Required Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__OpenWish` | (none) | PostgreSQL connection string. Format: `Server=HOST;Port=PORT;Database=DB;User Id=USER;Password=PASS` |
| `OpenWishSettings__OwnDatabaseUpgrades` | `false` | If `true`, automatically applies pending migrations on startup. Recommended: `true` for Docker deployments |

### Optional Configuration

#### Email (SMTP)

Configure email for password reset and notifications:

```bash
OpenWishSettings__EmailConfig__SmtpHost=smtp.gmail.com
OpenWishSettings__EmailConfig__SmtpPort=587
OpenWishSettings__EmailConfig__SmtpUser=your-email@gmail.com
OpenWishSettings__EmailConfig__SmtpPass=your-app-password
OpenWishSettings__EmailConfig__SmtpEnableTls=true
OpenWishSettings__EmailConfig__FromAddress=noreply@yourdomain.com
OpenWishSettings__EmailConfig__FromDisplayName="OpenWish"
```

**Important**: Use application-specific passwords, not your Gmail password. See [Gmail App Passwords](https://myaccount.google.com/apppasswords).

#### Google Sign-In (OAuth)

To enable Google sign-in:

1. **Create a Google OAuth application** at [Google Cloud Console](https://console.cloud.google.com/):
   - Create a new project
   - Enable the Google+ API
   - Create an OAuth 2.0 credential (Web application)
   - Add authorized redirect URIs: `https://yourdomain.com/signin-google`

2. **Configure environment variables**:
   ```bash
   OpenWishSettings__GoogleOAuthSettings__ClientId=your-client-id.apps.googleusercontent.com
   OpenWishSettings__GoogleOAuthSettings__ClientSecret=your-client-secret
   ```

#### TLS/SSL Certificate

For HTTPS in production:

```bash
# Option 1: Self-signed certificate (development only)
# See .docs/SELF_SIGNED_CERTIFICATE.md

# Option 2: Use a reverse proxy (recommended)
# Place nginx, Caddy, or Traefik in front of OpenWish
# They handle TLS termination
```

#### Logging

Control application logging verbosity:

```bash
Logging__LogLevel__Default=Information
Logging__LogLevel__OpenWish=Information
Logging__LogLevel__Microsoft=Warning
```

Valid levels: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`

## Database Management

### Connection Strings

**Local development**:
```
Server=localhost;Port=5432;Database=OpenWish;User Id=openwish;Password=YourPassword
```

**Docker Compose (same network)**:
```
Server=database;Port=5432;Database=OpenWish;User Id=openwish;Password=YourPassword
```

**Remote server**:
```
Server=db.example.com;Port=5432;Database=OpenWish;User Id=openwish;Password=YourPassword;SSL Mode=Require
```

### Backup Strategy

#### Regular Backups with pg_dump

1. **Create a backup script** (`backup-openwish.sh`):
   ```bash
   #!/bin/bash
   BACKUP_DIR="/var/backups/openwish"
   DATE=$(date +%Y%m%d_%H%M%S)
   
   mkdir -p $BACKUP_DIR
   docker exec openwish-postgres pg_dump -U openwish OpenWish | \
     gzip > $BACKUP_DIR/openwish_$DATE.sql.gz
   
   # Keep last 30 days of backups
   find $BACKUP_DIR -name "openwish_*.sql.gz" -mtime +30 -delete
   ```

2. **Schedule daily backups** with cron:
   ```bash
   0 2 * * * /path/to/backup-openwish.sh
   ```

#### Volume Backups

For complete system backups including database files:

```bash
docker-compose stop
tar -czf openwish-backup-$(date +%Y%m%d).tar.gz openwish-data/
docker-compose start
```

#### Restore from Backup

```bash
# Drop and recreate database
docker exec openwish-postgres dropdb -U openwish OpenWish
docker exec openwish-postgres createdb -U openwish OpenWish

# Restore from backup
zcat backup-file.sql.gz | docker exec -i openwish-postgres psql -U openwish OpenWish
```

### Database Maintenance

Monitor and maintain database health:

```bash
# Connect to PostgreSQL
docker exec -it openwish-postgres psql -U openwish OpenWish

# Useful queries:
# - Check database size:
SELECT pg_database.datname, 
       pg_size_pretty(pg_database_size(pg_database.datname)) 
FROM pg_database 
WHERE datname = 'OpenWish';

# - List active connections:
SELECT * FROM pg_stat_activity WHERE datname = 'OpenWish';

# - Run maintenance:
VACUUM ANALYZE;
```

## SSL/TLS Configuration

### Option 1: Self-Signed Certificate (Development)

See [.docs/SELF_SIGNED_CERTIFICATE.md](.docs/SELF_SIGNED_CERTIFICATE.md) for detailed instructions.

### Option 2: Reverse Proxy with Let's Encrypt (Recommended)

Use Caddy or nginx to handle TLS termination:

**Caddy example**:
```bash
# Install Caddy and create Caddyfile
caddy reverse-proxy --from https://openwish.example.com --to http://localhost:5001
```

Caddy automatically obtains and renews Let's Encrypt certificates.

### Option 3: Docker Reverse Proxy

Add a reverse proxy service to `docker-compose.yml`:

```yaml
reverse-proxy:
  image: caddy:latest
  container_name: openwish-caddy
  environment:
    DOMAIN: openwish.example.com
  volumes:
    - ./Caddyfile:/etc/caddy/Caddyfile
    - caddy-data:/data
    - caddy-config:/config
  ports:
    - "80:80"
    - "443:443"
  depends_on:
    - web
  restart: unless-stopped

volumes:
  caddy-data:
  caddy-config:
```

## Monitoring and Logging

### Container Logs

View application logs:

```bash
# Recent logs
docker-compose logs web

# Follow logs in real-time
docker-compose logs -f web

# Last 100 lines
docker-compose logs --tail=100 web

# Timestamp with logs
docker-compose logs --timestamps web
```

### Health Checks

OpenWish includes a health check endpoint:

```bash
curl http://localhost:5001/health
```

Expected response: HTTP 200 OK

### Metrics and Performance

Monitor resource usage:

```bash
# Container stats
docker stats openwish-web openwish-postgres

# Database connections
docker exec openwish-postgres psql -U openwish -d OpenWish \
  -c "SELECT count(*) FROM pg_stat_activity;"
```

## Common Troubleshooting

### Application Won't Start

**Issue**: Container exits immediately

**Solution**:
```bash
docker-compose logs web
# Check for database connection errors
# Verify ConnectionStrings__OpenWish is correct
```

### Database Connection Refused

**Issue**: "could not connect to database"

**Solution**:
```bash
# Verify database is running
docker-compose ps
docker-compose logs database

# Check connection string
docker exec openwish-web env | grep ConnectionStrings
```

### Slow Performance

**Issue**: Application is sluggish or unresponsive

**Solution**:
```bash
# Check resource usage
docker stats
# Consider increasing container memory/CPU limits

# Run database vacuum
docker exec openwish-postgres vacuumdb -U openwish OpenWish

# Check for long-running queries
docker exec openwish-postgres psql -U openwish -d OpenWish \
  -c "SELECT now() - query_start, query FROM pg_stat_activity WHERE state = 'active';"
```

### Email Not Sending

**Issue**: Password resets or notifications not arriving

**Solution**:
```bash
# Verify SMTP configuration
docker exec openwish-web env | grep SmtpHost
# Check application logs for SMTP errors
docker-compose logs web | grep -i smtp

# Test SMTP connection
docker exec openwish-postgres telnet smtp.gmail.com 587
```

### Users Unable to Log In

**Issue**: Login fails with "Invalid credentials"

**Solution**:
```bash
# Check user table
docker exec openwish-postgres psql -U openwish -d OpenWish \
  -c "SELECT Id, UserName, NormalizedUserName FROM AspNetUsers LIMIT 10;"

# Reset a user password (requires admin intervention)
# Delete the user and have them re-register
```

## Security Considerations

1. **Use strong passwords** for both PostgreSQL and email accounts
2. **Enable HTTPS** in production using a reverse proxy
3. **Restrict network access** to the database port (5432) to local containers only
4. **Keep OpenWish updated** to receive security patches
5. **Use application-specific passwords** for Google OAuth and email, not personal passwords
6. **Regularly backup** your database and store backups securely
7. **Monitor logs** for suspicious activity

## Performance Optimization

For groups with many wishlists, events, or users:

### Database Optimization

```yaml
database:
  environment:
    # Increase shared buffers for better performance
    POSTGRES_INIT_ARGS: "-c shared_buffers=256MB -c effective_cache_size=1GB"
```

### Container Resources

```yaml
web:
  deploy:
    resources:
      limits:
        cpus: '2'
        memory: 2G
      reservations:
        cpus: '1'
        memory: 1G
```

### Network Optimization

- Place the database and web container on the same Docker network
- Consider adding a local cache layer if product metadata import is slow

## Migration from Other Platforms

If you're migrating from another wishlist platform:

1. **Export data** from the old platform (CSV format if available)
2. **Create wishlists** in OpenWish and manually import items or
3. **Contact support** for custom migration assistance

OpenWish does not currently support automated imports from other platforms.

## Getting Help

- **Documentation**: Review [.docs/DEVELOPING.md](.docs/DEVELOPING.md) for development setup
- **GitHub Issues**: Report bugs at [mitch-b/OpenWish](https://github.com/mitch-b/OpenWish/issues)
- **Docker Hub**: [OpenWish container](https://github.com/mitch-b/OpenWish/pkgs/container/openwish-web)

## Version History

See [releases.json](src/OpenWish.Web/wwwroot/releases.json) for release notes and version history.
