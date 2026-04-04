# Convy — AI Governance Usage Guide

This document explains how to use the agents, skills, instructions, prompts, and hooks configured in this workspace for both **GitHub Copilot** (VS Code) and **Claude Code**.

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

The governance system is dual-compatible:

| Primitive | Copilot Location | Claude Code Location |
|-----------|-----------------|---------------------|
| Workspace instructions | `AGENTS.md` (root) | `CLAUDE.md` (root) |
| Context-specific instructions | `.github/instructions/*.instructions.md` | (reads AGENTS.md) |
| Agents | `.github/agents/*.agent.md` | (use as prompts) |
| Skills | `.github/skills/*/SKILL.md` | `.claude/skills/*/SKILL.md` |
| Prompts | `.github/prompts/*.prompt.md` | (use as prompts) |
| Hooks | `.github/hooks/*.json` | `.claude/settings.json` |

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
- Entities inherit from BaseEntity
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
| `docker.instructions.md` | `docker/**/docker-compose.yml` |

### Example

When editing `backend/src/Convy.Domain/Entities/ShoppingList.cs`, the AI automatically knows:
- Use `sealed record` for value objects
- Entities inherit from `BaseEntity`
- No EF Core or Infrastructure references allowed
- Use `Result<T>` for operations that can fail

---

## Agents

Agents (`.agent.md`) define specialized AI personas with specific tool access.

### How to Use (Copilot)

In VS Code Chat, mention an agent with `@`:

```
@backend-dev Create a new ShoppingList entity with Name, HouseholdId, and CreatedAt properties
```

```
@test-writer Write unit tests for the CreateShoppingListHandler
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
@backend-dev Add CQRS handlers for creating a shopping list item:
- CreateShoppingListItemCommand in Application
- Validator in Application
- Handler that uses IShoppingListRepository
```

The agent knows Clean Architecture rules and will generate code in the correct layers.

### Claude Code Equivalent

In Claude Code, paste the agent description as a prompt context or reference `AGENTS.md`:

```
Using the backend-dev agent rules from AGENTS.md, create...
```

---

## Skills

Skills (`.github/skills/*/SKILL.md` and `.claude/skills/*/SKILL.md`) are step-by-step workflows for complex multi-step tasks.

### How to Use (Copilot)

Reference a skill in a prompt:

```
Using the backend-feature skill, implement the Shopping List feature
```

Or Copilot loads them when the task description matches the skill.

### Available Skills

| Skill | Purpose | Steps |
|-------|---------|-------|
| `backend-feature` | Full feature (Domain→App→Infra→API→Tests) | 5 |
| `mobile-screen` | Full screen (MVI→Store→UI→Nav→DI→Preview) | 6 |
| `api-endpoint` | Single endpoint (lighter than backend-feature) | 4 |
| `db-migration` | EF Core migration with rollback test | 3 |
| `design-screen` | Design with Stitch MCP | 3 |
| `code-review` | Full review checklist | 5 |
| `test-suite` | Generate comprehensive test suite | 4 |

### Example: backend-feature Skill

```
@backend-dev Using the backend-feature skill, implement ShoppingList management:

Domain entities: ShoppingList (Id, Name, HouseholdId, CreatedAt, Items)
                 ShoppingListItem (Id, Name, Quantity, IsCompleted)

Commands: CreateShoppingList, AddItem, ToggleItemComplete
Queries: GetShoppingListsByHousehold, GetShoppingListById
```

The skill guides the agent through:
1. **Domain entities** → create in `Convy.Domain/Entities/`
2. **Application handlers** → create Commands/Queries with MediatR
3. **Infrastructure** → repository implementation with EF Core
4. **API endpoints** → minimal API routes
5. **Tests** → unit + integration tests

### Example: mobile-screen Skill

```
@mobile-dev Using the mobile-screen skill, create the Shopping List screen:

State: list of ShoppingList, loading flag, error message
Intents: LoadLists, CreateList(name), DeleteList(id)
SideEffects: NavigateToDetail(id), ShowError(message)
```

### Claude Code Equivalent

Claude Code reads skills from `.claude/skills/*/SKILL.md` automatically. Reference by name:

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
/new-feature Shopping Lists
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
feature: Shopping Lists
description: Users can create, share, and manage shopping lists within a household
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

MCP (Model Context Protocol) servers extend AI capabilities.

| Server | Purpose | Use Case |
|--------|---------|----------|
| GitHub | Repo management | PRs, issues, code search |
| PostgreSQL | DB interaction | Query, inspect schema |
| Stitch | UI design | Generate screen mockups |
| Context7 | Library docs | Look up API references |

### Configuration

- Copilot: `.vscode/settings.json` → `mcp.servers`
- Claude: `.claude/settings.json` → `mcpServers`

See [docs/MCP-SETUP.md](MCP-SETUP.md) for server-specific setup instructions.

---

## Examples

### Full Workflow: Adding a new feature

1. **Design** the screen:
   ```
   @ui-designer Using the design-screen skill, design a Shopping List screen with:
   - List of shopping items with checkboxes
   - FAB to add new item
   - Swipe to delete
   ```

2. **Implement backend**:
   ```
   @backend-dev Using the backend-feature skill, implement ShoppingList CRUD
   ```

3. **Implement mobile**:
   ```
   @mobile-dev Using the mobile-screen skill, create ShoppingListScreen
   ```

4. **Write tests**:
   ```
   @test-writer Using the test-suite skill, add tests for ShoppingList feature
   ```

5. **Review**:
   ```
   @reviewer Review the ShoppingList feature implementation
   ```

### Quick: Adding a single API endpoint

```
@backend-dev Using the api-endpoint skill, add GET /api/v1/households/{id}/shopping-lists
```

### Quick: Fix a bug

```
/fix-bug
description: Shopping list items not syncing in real-time
steps_to_reproduce: Create item on device A, device B doesn't see it
```

### Quick: Run EF Core migration

```
@db-architect Using the db-migration skill, add migration for ShoppingList and ShoppingListItem tables
```
