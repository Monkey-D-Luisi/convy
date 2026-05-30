# ADR 007: Worker Boundary

## Status

Accepted

## Context

Convy previously ran synchronous API handlers plus in-process hosted services for scheduled work. Task reminders, recurring item rollover, and metric snapshots need to run without a user request and should not share API request latency or lifecycle.

## Decision

Add `Convy.Worker` in this remediation PR as a dedicated .NET worker process.

The worker owns:

- recurring item rollover
- task reminder fanout
- system metric snapshots

The API continues to own HTTP endpoints, OAuth, SignalR, and its in-process push notification batcher for immediate household events. The worker does not expose public HTTP endpoints.

Task reminders use a PostgreSQL advisory lock so multiple worker instances do not send duplicate reminders. Recurring item rollover remains idempotent at the domain/repository behavior level and is covered by regression tests.

Future worker jobs must define queue ownership, idempotency, retry policy, dead-letter handling, observability, and deployment scaling before implementation.

## Consequences

API startup is no longer responsible for scheduled jobs. Deployments must run the `worker` Compose service alongside `api`; operational checks include worker logs and restart state. CI builds and tests `Convy.Worker` with the backend solution and validates the worker Docker image.
