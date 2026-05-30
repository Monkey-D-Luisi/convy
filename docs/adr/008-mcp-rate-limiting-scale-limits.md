# ADR 008: MCP Rate Limiting Scale Limits

## Status

Accepted

## Context

The MCP service currently uses an in-process fixed-window limiter. That is sufficient for a single controlled-release instance but does not coordinate limits across multiple MCP replicas.

## Decision

Keep the in-process limiter for the current deployment. Treat multi-instance MCP as requiring an external rate-limit store or gateway policy before scaling out.

Scale-out criteria:

- more than one MCP replica
- public onboarding beyond controlled release
- sustained traffic where per-process limits are insufficient
- abuse patterns that require IP, OAuth client, or user-level global limits

## Consequences

The deployment runbook must not scale MCP horizontally without adding distributed rate limiting. Current tests and docs describe the fixed-window limiter as a single-instance control.
