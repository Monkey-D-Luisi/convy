# Convy — Claude Code Project Instructions

## Task Completion Signal

When you have **fully completed** a task (all steps done, tests passing):

```powershell
Set-Content -Path "C:\Users\luiss\openclaw-signal.txt" -Value "DONE:convy:<HH:mm> - <one-line summary>"
```

Do **not** write this file mid-task — only when truly finished.

## Plan Ready Signal

When you have a plan ready for review:

```powershell
Set-Content -Path "C:\Users\luiss\openclaw-signal.txt" -Value "PLAN_READY:convy:<HH:mm> - <one-line summary>"
```

## Language

All code, variables, functions, comments, commits, branches, and docs in **English**.

## Code Principles

- Follow **SOLID** strictly.
- Composition over inheritance.
- No premature abstractions — helpers only with 2+ concrete use cases.
- No dead code or commented-out code.
- Don't add features or refactoring beyond what was requested.
- Validate inputs only at system boundaries.

## Architecture

- **Backend:** Clean Architecture (Onion) + CQRS via MediatR in `backend/`.
  - Domain → Application → Infrastructure → API (dependency direction inward).
  - Domain layer has ZERO external dependencies.
- **Mobile:** MVI (Model-View-Intent) + Compose Multiplatform in `mobile/`.
- See `docs/ARCHITECTURE.md` for full details.

## Naming Conventions

| Context | Convention | Example |
|---------|-----------|---------|
| C# classes/methods | PascalCase | `HouseholdService` |
| C# private fields | _camelCase | `_repository` |
| C# interfaces | I-prefix | `IHouseholdRepository` |
| Kotlin classes | PascalCase | `ShoppingListStore` |
| Kotlin functions | camelCase | `loadItems()` |
| DB tables | snake_case plural | `list_items` |
| DB columns | snake_case | `created_by` |
| API endpoints | kebab-case | `/api/v1/list-items` |

## Git Conventions

- Conventional Commits: `feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`
- Branch naming: `feature/xxx`, `fix/xxx`, `chore/xxx`

## Build & Test

```bash
# Backend
cd backend && dotnet restore && dotnet build && dotnet test

# Mobile
cd mobile && ./gradlew :composeApp:assembleDebug

# Infrastructure
docker-compose up -d
```

## Key References

- Product spec: `docs/mvp-spec.md`
- Architecture: `docs/ARCHITECTURE.md`
- Testing: `docs/TESTING.md`

## Governance

Respect the layered governance:
1. This file (global standards)
2. Subfolder AGENTS.md files (`backend/AGENTS.md`, `mobile/AGENTS.md`)
3. `.claude/skills/*/SKILL.md` for workflow procedures
4. `.claude/settings.json` for hooks

Skills are available in `.claude/skills/` and `.github/skills/` — both locations are valid.
