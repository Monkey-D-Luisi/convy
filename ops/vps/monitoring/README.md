# VPS Monitoring

Install the external health check timer after a healthy deploy:

```bash
sudo bash /opt/convy/current/ops/vps/monitoring/install-monitoring-timers.sh
```

Optional environment file:

```bash
sudo install -m 600 -o root -g root /dev/null /opt/convy/shared/monitoring.env
```

Supported values:

- `ALERT_WEBHOOK_URL`
- `ALERT_WEBHOOK_BEARER`
- `API_HEALTH_URL`
- `AUTH_HEALTH_URL`
- `MCP_HEALTH_URL`
- `EXTRA_HEALTH_URLS`
