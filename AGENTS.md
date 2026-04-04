# Convy — Project Guidelines

## Language

All code, variable names, function names, comments, commit messages, branch names, and documentation must be written in **English**. The product spec is in Spanish for stakeholder communication only.

## Code Principles

- Follow **SOLID** principles strictly.
- Prefer composition over inheritance.
- No premature abstractions — create helpers only when there are 2+ concrete use cases.
- No dead code, no commented-out code in commits.
- Don't add features, refactoring, or "improvements" beyond what was explicitly requested.
- Don't add docstrings or comments to code you didn't change.
- Validate inputs only at system boundaries (API endpoints, external data ingestion).

## Architecture

- **Backend:** Clean Architecture (Onion) with CQRS via MediatR. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
- **Mobile:** MVI (Model-View-Intent) with Compose Multiplatform. See `mobile/AGENTS.md`.
- **Domain layer has ZERO external dependencies.** Application depends on Domain. Infrastructure depends on Application + Domain. API depends on all.

## Naming Conventions

| Context | Convention | Example |
|---------|-----------|---------|
| C# classes/methods | PascalCase | `HouseholdService`, `CreateItem` |
| C# private fields | _camelCase | `_repository` |
| C# interfaces | I-prefix | `IHouseholdRepository` |
| Kotlin classes | PascalCase | `ShoppingListStore` |
| Kotlin functions/props | camelCase | `loadItems()`, `isCompleted` |
| Compose composables | PascalCase | `ItemCard()`, `ListScreen()` |
| DB tables | snake_case plural | `list_items`, `households` |
| DB columns | snake_case | `created_by`, `completed_at` |
| API endpoints | kebab-case | `/api/v1/list-items` |

## Git Conventions

- **Commit format:** [Conventional Commits](https://www.conventionalcommits.org/)
  - `feat:` new feature
  - `fix:` bug fix
  - `chore:` maintenance
  - `docs:` documentation
  - `test:` adding/fixing tests
  - `refactor:` code change that neither fixes nor adds
- **Branch naming:** `feature/<short-description>`, `fix/<short-description>`, `chore/<short-description>`
- **PRs:** always to `main`, require review.

## Build & Test

### Backend
```bash
cd backend
dotnet restore
dotnet build
dotnet test
```

### Mobile
```bash
cd mobile
./gradlew :composeApp:assembleDebug
./gradlew :shared:allTests
```

### Infrastructure
```bash
docker-compose up -d    # PostgreSQL + pgAdmin
```

## Key References

- Product spec: [docs/mvp-spec.md](docs/mvp-spec.md)
- Architecture: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- Testing: [docs/TESTING.md](docs/TESTING.md)
- Usage guide: [docs/USAGE.md](docs/USAGE.md)

## Governance

This project uses layered AI governance. The agent MUST respect:

1. **This file** for global standards
2. **Subfolder `AGENTS.md`** files for layer-specific rules (`backend/AGENTS.md`, `mobile/AGENTS.md`)
3. **`.github/instructions/*.instructions.md`** for file-pattern-specific rules
4. **`.github/skills/*/SKILL.md`** for workflow procedures
5. **`.github/hooks/`** for deterministic guardrails

Never bypass governance. If a governance rule conflicts with a user request, flag it rather than silently ignore it.
