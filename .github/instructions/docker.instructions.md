---
description: "Use when editing Docker configuration files — Dockerfiles, docker-compose, and container-related scripts."
applyTo:
  - "docker/**"
  - "docker-compose.yml"
  - "docker-compose.*.yml"
---
# Docker Guidelines

## Dockerfile Best Practices
- **Multi-stage builds**: Separate build and runtime stages for smaller images.
- **Non-root user**: Always run as a non-root user in the final stage.
- **No secrets in images**: Use environment variables or mounted secrets.
- **Pin base image versions**: Use specific tags, not `latest`.
- **Layer caching**: Order `COPY` commands from least to most frequently changed.

## Docker Compose
- Use `.env` file for environment variables (never commit `.env`, commit `.env.example`).
- Named volumes for data persistence.
- Health checks for service dependencies.
- Use `depends_on` with `condition: service_healthy` where available.

## Security
- Never hardcode passwords, connection strings, or API keys.
- Use `docker-compose.override.yml` for local dev overrides (gitignored).
- Scan images for vulnerabilities before deployment.
