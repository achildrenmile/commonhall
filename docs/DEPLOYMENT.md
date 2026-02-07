# CommonHall Deployment Guide

## System Requirements

### Minimum Requirements (Small Team: 1-50 users)

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| **CPU** | 4 cores | 8 cores |
| **RAM** | 8 GB | 16 GB |
| **Disk** | 50 GB SSD | 100 GB SSD |
| **Network** | 100 Mbps | 1 Gbps |

### Resource Allocation by Service

| Service | CPU (min) | CPU (max) | RAM (min) | RAM (max) |
|---------|-----------|-----------|-----------|-----------|
| **nginx** | 0.1 | 0.5 | 64 MB | 256 MB |
| **api** | 0.5 | 2.0 | 512 MB | 2 GB |
| **web** | 0.25 | 1.0 | 256 MB | 1 GB |
| **postgres** | 0.5 | 2.0 | 512 MB | 2 GB |
| **redis** | 0.1 | 1.0 | 128 MB | 768 MB |
| **elasticsearch** | 0.5 | 2.0 | 1 GB | 2 GB |
| **Total** | **1.95** | **8.5** | **2.5 GB** | **8 GB** |

### Scaling Guidelines

| Users | CPU Cores | RAM | Notes |
|-------|-----------|-----|-------|
| 1-50 | 4 | 8 GB | Single instance, all services |
| 50-200 | 8 | 16 GB | May need ES heap increase |
| 200-500 | 16 | 32 GB | Consider separating DB |
| 500+ | 32+ | 64+ GB | Multi-node, load balanced |

---

## Production Profiles

### Small Profile (4 CPU / 8 GB RAM)
Optimized for small teams or testing. Uses `docker-compose.prod.small.yml`.

```yaml
# Resource allocation
api:        0.5 CPU,  512 MB RAM
web:        0.25 CPU, 256 MB RAM
postgres:   0.5 CPU,  512 MB RAM
redis:      0.1 CPU,  128 MB RAM
elasticsearch: 0.5 CPU, 1 GB RAM (512 MB heap)
nginx:      0.1 CPU,  64 MB RAM
---
Total:      ~2 CPU,   ~2.5 GB RAM (leaves headroom)
```

### Standard Profile (8 CPU / 16 GB RAM)
Default production configuration. Uses `docker-compose.prod.yml`.

```yaml
# Resource allocation
api:        2 CPU,    2 GB RAM
web:        1 CPU,    1 GB RAM
postgres:   2 CPU,    2 GB RAM
redis:      1 CPU,    768 MB RAM
elasticsearch: 2 CPU, 2 GB RAM (1 GB heap)
nginx:      0.5 CPU,  256 MB RAM
---
Total:      ~8.5 CPU, ~8 GB RAM
```

---

## Deployment to host-node-01

### Server Specifications
```
Host: host-node-01
CPU:  4 cores (Intel i5-6500T @ 2.50GHz)
RAM:  16 GB (14 GB available)
Disk: 259 GB available
Docker: 29.2.0
```

**Verdict: ✅ Suitable for Small Profile deployment**

### Deployment Steps

1. **Clone repository on server**
   ```bash
   ssh achildrenmile@host-node-01
   git clone https://github.com/achildrenmile/commonhall.git
   cd commonhall
   ```

2. **Configure environment**
   ```bash
   cp .env.example .env
   # Edit .env with production values
   nano .env
   ```

3. **Build images on server** (or pull from registry)
   ```bash
   # Build locally
   make prod-build

   # Or pull from registry
   docker pull ghcr.io/achildrenmile/commonhall-api:latest
   docker pull ghcr.io/achildrenmile/commonhall-web:latest
   ```

4. **Start with small profile**
   ```bash
   docker compose -f infrastructure/docker/docker-compose.prod.small.yml up -d
   ```

5. **Run migrations**
   ```bash
   docker compose -f infrastructure/docker/docker-compose.prod.small.yml exec api \
     dotnet CommonHall.Api.dll --migrate
   ```

6. **Verify deployment**
   ```bash
   # Check all services
   docker compose -f infrastructure/docker/docker-compose.prod.small.yml ps

   # Check health
   curl http://localhost/api/health
   curl http://localhost
   ```

---

## Storage Requirements

### Disk Space Estimates

| Component | Initial | Per 100 users/year |
|-----------|---------|-------------------|
| PostgreSQL data | 100 MB | 1-5 GB |
| Elasticsearch indices | 200 MB | 2-10 GB |
| File uploads | 0 | 5-20 GB |
| Redis persistence | 50 MB | 100-500 MB |
| Docker images | 3 GB | - |
| Logs | 100 MB | 1-5 GB |
| **Total** | **~3.5 GB** | **10-40 GB/year** |

### Backup Storage
Plan for 3x production data size for backups (daily + weekly retention).

---

## Network Requirements

### Ports
| Port | Service | External |
|------|---------|----------|
| 80 | HTTP (nginx) | Yes |
| 443 | HTTPS (nginx) | Yes |
| 5432 | PostgreSQL | No (internal) |
| 6379 | Redis | No (internal) |
| 9200 | Elasticsearch | No (internal) |

### Firewall Rules
```bash
# Allow HTTP/HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Block direct access to internal services
sudo ufw deny 5432/tcp
sudo ufw deny 6379/tcp
sudo ufw deny 9200/tcp
```

---

## SSL/TLS Setup

### Using Let's Encrypt (Certbot)
```bash
# Install certbot
sudo apt install certbot

# Get certificate
sudo certbot certonly --standalone -d commonhall.yourdomain.com

# Copy to docker volume
sudo cp /etc/letsencrypt/live/commonhall.yourdomain.com/fullchain.pem \
  infrastructure/docker/ssl/
sudo cp /etc/letsencrypt/live/commonhall.yourdomain.com/privkey.pem \
  infrastructure/docker/ssl/
```

### Enable HTTPS in nginx.conf
Uncomment the HTTPS server block in `infrastructure/nginx/nginx.conf`.

---

## Monitoring

### Health Check Endpoints
- `GET /api/health` - Basic health (returns 200 if API is running)
- `GET /api/health/ready` - Ready check (includes DB, Redis, ES status)

### Log Locations
```bash
# API logs
docker logs commonhall-api

# All service logs
docker compose -f infrastructure/docker/docker-compose.prod.small.yml logs -f

# Persistent logs (if configured)
/var/lib/docker/volumes/commonhall_api-logs/_data/
```

### Recommended Monitoring Stack
For production, consider adding:
- **Prometheus** + **Grafana** for metrics
- **Loki** for log aggregation
- **Uptime Kuma** for availability monitoring

---

## Troubleshooting

### Elasticsearch Won't Start
```bash
# Check vm.max_map_count (required for ES)
sysctl vm.max_map_count

# If less than 262144, increase it
sudo sysctl -w vm.max_map_count=262144

# Make persistent
echo "vm.max_map_count=262144" | sudo tee -a /etc/sysctl.conf
```

### Out of Memory
```bash
# Check container memory usage
docker stats

# Reduce ES heap if needed (edit docker-compose)
ES_JAVA_OPTS=-Xms512m -Xmx512m
```

### Database Connection Issues
```bash
# Check postgres is healthy
docker exec commonhall-postgres pg_isready

# Check connection string
docker exec commonhall-api env | grep ConnectionStrings
```
