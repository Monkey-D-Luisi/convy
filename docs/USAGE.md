# Convy — AI Governance Usage Guide

This document explains how to use the agents, skills, instructions, prompts, and hooks configured in this workspace for **GitHub Copilot** (VS Code), **Claude Code**, and **Codex**.

## Table of Contents

- [Overview](#overview)
- [Instructions](#instructions)
- [Agents](#agents)
- [Skills](#skills)
- [Prompts](#prompts)
- [Hooks](#hooks)
- [MCP Servers](#mcp-servers)
- [Examples](#examples)

---

## Overview

The governance system is multi-editor compatible. `.github/skills/` is the canonical skill source; `.claude/skills/` and `.agents/skills/` are synchronized copies for editor-specific discovery.

| Primitive | Copilot Location | Claude Code Location | Codex Location |
|-----------|------------------|----------------------|----------------|
| Workspace instructions | `AGENTS.md` | `CLAUDE.md` plus `AGENTS.md` | `AGENTS.md` |
| Context-specific instructions | `.github/instructions/*.instructions.md` | `AGENTS.md` and subfolder `AGENTS.md` files | `AGENTS.md` and subfolder `AGENTS.md` files |
| Agents | `.github/agents/*.agent.md` | Use as prompts when needed | Use as prompts when needed |
| Skills | `.github/skills/*/SKILL.md` | `.claude/skills/*/SKILL.md` | `.agents/skills/*/SKILL.md` |
| Prompts | `.github/prompts/*.prompt.md` | Use as prompts | Use as prompts |
| Hooks | `.github/hooks/*.json` | `.claude/settings.json` | Project and app-level guardrails |

---

## Instructions

Instructions files (`.instructions.md`) provide context-specific rules that activate automatically based on file patterns.

### How They Work

Each file has YAML frontmatter with an `applyTo` glob pattern:

```yaml
---
applyTo: "backend/src/Convy.Domain/**"
---
# Domain Layer Rules
- No external dependencies
- Entities inherit from Entity
```

When you edit a file matching `backend/src/Convy.Domain/**`, these rules are automatically injected into the AI context.

### Available Instructions

| File | Activates For |
|------|--------------|
| `dotnet-domain.instructions.md` | `backend/src/Convy.Domain/**` |
| `dotnet-application.instructions.md` | `backend/src/Convy.Application/**` |
| `dotnet-infrastructure.instructions.md` | `backend/src/Convy.Infrastructure/**` |
| `dotnet-api.instructions.md` | `backend/src/Convy.API/**` |
| `dotnet-tests.instructions.md` | `backend/tests/**` |
| `kotlin-compose.instructions.md` | `mobile/**/*.kt` |
| `ef-migrations.instructions.md` | On-demand (description trigger) |
| `docker.instructions.md` | `docker/**`, `docker-compose*.yml` |

### Example

When editing `backend/src/Convy.Domain/Entities/HouseholdList.cs`, the AI automatically knows:
- Use `sealed record` for value objects
- Entities inherit from `Entity`
- No EF Core or Infrastructure references allowed
- Domain entities enforce invariants; application handlers use `Result<T>` for expected failures

---

## Agents

Agents (`.agent.md`) define specialized AI personas with specific tool access.

### How to Use (Copilot)

In VS Code Chat, mention an agent with `@`:

```
@backend-dev Add backend support for archiving household lists
```

```
@test-writer Write unit tests for ArchiveListCommandHandler
```

```
@reviewer Review the changes in this PR for SOLID violations
```

### Available Agents

| Agent | Purpose | Tools |
|-------|---------|-------|
| `@backend-dev` | Backend feature development | read, edit, search, execute |
| `@mobile-dev` | Mobile UI and logic | read, edit, search, execute |
| `@db-architect` | DB schema and migrations | read, edit, search, execute, postgres-mcp |
| `@ui-designer` | Design screens with Stitch | read, search, stitch MCP |
| `@reviewer` | Code review (read-only) | read, search |
| `@test-writer` | Test generation | read, edit, search, execute |

### Example: Using @backend-dev

```
@backend-dev Add CQRS handlers for creating a household list item:
- CreateItemCommand in Application
- Validator in Application
- Handler that uses IListItemRepository
```

The agent knows Clean Architecture rules and will generate code in the correct layers.

### Claude Code Equivalent

In Claude Code, paste the agent description as a prompt context or reference `AGENTS.md`:

```
Using the backend-dev agent rules from AGENTS.md, create...
```

---

## Skills

Skills are step-by-step workflows for complex multi-step tasks. Keep all three skill directories synchronized:

- `.github/skills/*/SKILL.md` for Copilot and the canonical source
- `.claude/skills/*/SKILL.md` for Claude Code
- `.agents/skills/*/SKILL.md` for Codex

### How to Use (Copilot)

Reference a skill in a prompt:

```
Using the backend-feature skill, implement task item management
```

Or Copilot loads them when the task description matches the skill.

### Available Skills

| Skill | Purpose | Steps |
|-------|---------|-------|
| `backend-feature` | Full feature (Domain->Application->Infrastructure->API->Tests->Verify) | 6 |
| `mobile-screen` | Full screen (MVI->Store->UI->Navigation->DI->Preview->Verify) | 7 |
| `api-endpoint` | Single endpoint (lighter than backend-feature) | 7 |
| `db-migration` | EF Core migration with rollback test | 3 |
| `design-screen` | Design with Stitch MCP | 6 |
| `firebase-setup` | Firebase Auth setup and verification | 5 |
| `code-review` | Full review checklist | 5 |
| `test-suite` | Generate comprehensive test suite | 5 |

### Example: backend-feature Skill

```
@backend-dev Using the backend-feature skill, implement task item management:

Domain entities: TaskItem (Id, Title, Note, ListId, CreatedBy, IsCompleted)

Commands: CreateTask, UpdateTask, CompleteTask, UncompleteTask, DeleteTask
Queries: GetListTasks
```

The skill guides the agent through:
1. **Domain entities** → create in `Convy.Domain/Entities/`
2. **Application handlers** → create Commands/Queries with MediatR
3. **Infrastructure** → repository implementation with EF Core
4. **API endpoints** → minimal API routes
5. **Tests** → unit + integration tests

### Example: mobile-screen Skill

```
@mobile-dev Using the mobile-screen skill, create the List Detail screen:

State: list of pending items, list type, loading flag, error message
Intents: LoadItems, CompleteItem(id), DeleteItem(id), Refresh
SideEffects: NavigateToItemForm(id), ShowError(message)
```

### Claude Code Equivalent

Claude Code reads skills from `.claude/skills/*/SKILL.md`. Codex reads project skills from `.agents/skills/*/SKILL.md`. Reference by name:

```
Use the backend-feature skill to implement...
```

---

## Prompts

Prompts (`.prompt.md`) are reusable templates for common tasks with placeholder variables.

### How to Use (Copilot)

Open the Command Palette → **Copilot: Use Prompt** → select a prompt. Variables like `{{feature}}` will be filled in by the user.

Or reference in chat:

```
/new-feature Household list archiving
```

### Available Prompts

| Prompt | Purpose | Variables |
|--------|---------|-----------|
| `new-feature` | Full feature across backend + mobile | `feature`, `description` |
| `fix-bug` | Investigate and fix a bug | `description`, `steps_to_reproduce` |
| `add-test` | Add tests for existing code | `target_code`, `test_type` |
| `design-ui` | Design a screen with Stitch MCP | `screen_name`, `description` |
| `review-pr` | Full code review | `pr_number` |

### Example: new-feature Prompt

Trigger: `/new-feature`

Input:
```
feature: Household list archiving
description: Users can archive lists that are no longer active without deleting their history
```

The prompt orchestrates the backend-dev and mobile-dev agents with the backend-feature and mobile-screen skills to scaffold the full feature.

---

## Hooks

Hooks are guardrails that run before/after tool executions to enforce architecture rules.

### Layer Guard Hook

Prevents the Domain layer from importing Infrastructure namespaces.

**What it blocks:**
- Adding `using Microsoft.EntityFrameworkCore;` in Domain files
- Importing `Convy.Infrastructure` in Domain files

**What happens:** The AI receives a deny message and must restructure the code to respect the dependency rule.

### Copilot Location
`.github/hooks/layer-guard.json`

### Claude Code Location
`.claude/settings.json` → `hooks` section

---

## MCP Servers

This section is about AI development MCP tooling used by coding agents. It is separate from the Convy ChatGPT MCP integration documented in [docs/mcp](mcp/README.md).

MCP (Model Context Protocol) servers extend AI development capabilities.

| Server | Purpose | Use Case |
|--------|---------|----------|
| GitHub | Repo management | PRs, issues, code search |
| PostgreSQL | DB interaction | Query, inspect schema |
| Stitch | UI design | Generate screen mockups |
| Context7 | Library docs | Look up API references |
| Maestro | Android E2E automation | Run flows, inspect hierarchy, take screenshots |

### Configuration

- Copilot: `.vscode/settings.json` → `mcp.servers`
- Claude: `.claude/settings.json` → `mcpServers`

See [docs/ai-tooling/mcp-setup.md](ai-tooling/mcp-setup.md) for server-specific setup instructions. The legacy [docs/MCP-SETUP.md](MCP-SETUP.md) file is only a compatibility stub.

---

## Examples

### Full Workflow: Adding a new feature

1. **Design** the screen:
   ```
   @ui-designer Using the design-screen skill, design a List Detail screen with:
   - Pending items with checkboxes
   - FAB to add new item
   - Swipe to delete
   ```

2. **Implement backend**:
   ```
   @backend-dev Using the backend-feature skill, implement list archiving
   ```

3. **Implement mobile**:
   ```
   @mobile-dev Using the mobile-screen skill, update HouseholdListsScreen archive behavior
   ```

4. **Write tests**:
   ```
   @test-writer Using the test-suite skill, add tests for list archiving
   ```

5. **Review**:
   ```
   @reviewer Review the list archiving implementation
   ```

### Quick: Adding a single API endpoint

```
@backend-dev Using the api-endpoint skill, add GET /api/v1/households/{id}/lists
```

### Quick: Fix a bug

```
/fix-bug
description: Shopping list items not syncing in real-time
steps_to_reproduce: Create item on device A, device B doesn't see it
```

### Quick: Run EF Core migration

```
@db-architect Using the db-migration skill, add migration for notification preferences
```
