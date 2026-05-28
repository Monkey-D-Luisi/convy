# Convy ChatGPT MCP Beta

This directory documents the private ChatGPT MCP beta.

## Components

- `auth/`: public OAuth authorization surface at `https://auth.convyapp.com`.
- `backend/src/Convy.API`: OAuth broker, token endpoint, revoke endpoint, scope enforcement, and audit ingestion.
- `mcp/`: tool-only MCP service at `https://mcp.convyapp.com`.
- `docker/Caddyfile.vps`: public routing for `convyapp.com`, `auth.convyapp.com`, and `mcp.convyapp.com`.

## Operations

- Domain cutover: [domain-cutover.md](domain-cutover.md)
- Public ChatGPT submission prep: [public-submission.md](public-submission.md)

## Beta Scope

The beta exposes these read scopes:

- `convy.households.read`
- `convy.lists.read`
- `convy.items.read`
- `convy.tasks.read`
- `convy.activity.read`

The beta exposes these limited write scopes:

- `convy.items.write`
- `convy.tasks.write`

Write tools can only create and complete/uncomplete shopping items and tasks. They are idempotent, non-destructive, and cannot edit, delete, archive, invite, leave, manage lists, view admin metrics, or access backups.
