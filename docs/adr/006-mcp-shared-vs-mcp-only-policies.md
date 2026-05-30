# ADR 006: MCP Shared vs MCP-Only Policies

## Status

Accepted

## Context

Some API endpoints are safe for Firebase mobile users and MCP access tokens, while smart write endpoints must only accept MCP access tokens with explicit MCP write scopes.

## Decision

Keep shared read policies for endpoints that are valid for Firebase and MCP callers. Add MCP-only policy names for smart write endpoints, backed only by the MCP bearer authentication scheme.

MCP-only policies are used for:

- item smart batch writes
- item status batch writes
- task smart batch writes
- task status batch writes

Shared read policies remain usable by Firebase/mobile callers.

## Consequences

Firebase tokens cannot call MCP smart write endpoints even if they reach the API route. MCP read behavior remains reusable without duplicating read endpoints.
