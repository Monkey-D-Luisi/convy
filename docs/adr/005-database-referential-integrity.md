# ADR 005: Database Referential Integrity

## Status

Accepted

## Context

Core Convy tables previously relied on application logic for several relationships. That allowed orphaned rows if a bug, migration, or manual operation bypassed normal command handlers.

## Decision

Add database foreign keys for core ownership and actor references.

Structural ownership cascades on delete:

- `households -> household_lists`
- `households -> invites`
- `household_lists -> list_items`
- `household_lists -> task_items`
- `users -> device_tokens`
- `users -> notification_preferences`

Actor and audit references restrict deletion:

- `created_by`
- `completed_by`
- `assigned_to_user_id`
- `returned_to_pending_by`
- `performed_by`

Operators must run `ops/vps/db/check-orphan-references.sql` before applying the FK migration to an existing environment.

## Consequences

The database now rejects orphan writes even when they bypass application handlers. User deletion requires explicit cleanup or retention decisions for audit-linked rows before deleting referenced users.
