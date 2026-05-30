# Convy Overview

Convy helps households coordinate recurring home work with low friction. It focuses on shared lists, task lists, realtime collaboration, voice-assisted item capture, push notifications, and a controlled-release ChatGPT MCP connector.

## Users

- Household members who need shared shopping lists and tasks.
- The project maintainer, who operates the controlled-release environment and dashboard.
- Technical reviewers who need to understand architecture, security, deployment, and operations.
- AI coding agents that need stable project context without reading the whole repository.

## Main Modules

| Module | Purpose |
| --- | --- |
| Mobile app | Android-first user app for auth, households, lists, tasks, voice input, realtime updates, and settings. |
| Backend API | REST, SignalR, Firebase token validation, CQRS handlers, OpenAI voice parsing, admin metrics, OAuth broker, MCP audit. |
| Worker | .NET background process for recurring item rollover, task reminders, and system metric snapshots. |
| Dashboard | Admin-only Next.js UI for operational health, usage metrics, OpenAI metrics, MCP metrics, backups, and system status. |
| Auth app | Public Next.js OAuth authorization surface for ChatGPT MCP consent. |
| MCP service | ChatGPT MCP resource server exposing scoped Convy tools through the backend API. |
| Public site | Minimal landing page for `convyapp.com`. |
| Legal site | Static privacy and terms pages for `legal.convyapp.com`. |
| Operations | Docker Compose, Caddy, Terraform, deployment, secret push, backup, and restore scripts. |

## Capabilities

- Households: create, rename, switch, join through invites, and leave where supported by the app/API.
- Lists: shopping and task list management with archived-list behavior.
- Items: create, complete, uncomplete, search/filter, shopping mode, recurrence metadata, and undo-oriented mobile flows.
- Tasks: create, complete, uncomplete, assign, prioritize, and manage due date/reminder metadata.
- Voice: OpenAI transcription and parsing for household item capture, with redacted operational metrics.
- Realtime: SignalR updates scoped by household.
- Push: Firebase Cloud Messaging device registration and notification preferences.
- Worker jobs: scheduled recurring items, task reminders, and system metrics run outside API request handling.
- Dashboard: operational health, usage, OpenAI, MCP, backups, and system status.
- Backups: local PostgreSQL custom-format dumps, checksum metadata, catalog verification, scheduled restore verification, and optional encrypted restic offsite upload on the VPS.
- ChatGPT MCP: scoped read tools and limited idempotent item/task write tools through OAuth.

## Environment State

The active hosted controlled-release path is Hetzner VPS plus Docker Compose and Caddy. OCI files remain as fallback/reference material unless explicitly updated for parity. Legacy `nip.io` hosts stay configured for installed staging Android builds; new staging Android builds default to `api.convyapp.com`.

Deep references:

- [Architecture](ARCHITECTURE.md)
- [Development](DEVELOPMENT.md)
- [Testing](TESTING.md)
- [Deployment](DEPLOYMENT.md)
- [Operations](OPERATIONS.md)
- [Security](SECURITY.md)
- [ChatGPT MCP](mcp/README.md)
