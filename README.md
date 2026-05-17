# Convy

> Coordinate your home in seconds, with minimal friction.

**Convy** is a shared mobile app for households: fast lists, tasks, and errands with real-time sync.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 10, PostgreSQL, SignalR |
| Mobile | Kotlin Multiplatform + Compose Multiplatform (Android first) |
| Auth | Firebase Auth (token validation on backend) |
| Architecture | Clean Architecture + CQRS (backend), MVI (mobile) |
| Infrastructure | Docker Compose (local), GitHub Actions (CI) |

## Repository Structure

```
convy/
|-- .github/              # Copilot governance, prompts, hooks, and GitHub Actions
|-- .claude/              # Claude Code governance and MCP configuration
|-- .agents/              # Codex agent workflow skills
|-- backend/              # ASP.NET Core solution (Clean Architecture)
|   |-- src/
|   |   |-- Convy.Domain/
|   |   |-- Convy.Application/
|   |   |-- Convy.Infrastructure/
|   |   `-- Convy.API/
|   `-- tests/
|-- mobile/               # Kotlin Multiplatform + Compose Multiplatform
|   |-- androidApp/       # Android application entry point and flavors
|   |-- composeApp/       # Shared Compose UI
|   `-- shared/           # Shared domain, data, networking, and DI
|-- docker/               # Local and production Docker Compose files
|-- docs/                 # Product, architecture, testing, and governance docs
|-- AGENTS.md             # Cross-editor workspace instructions
`-- CLAUDE.md             # Claude Code project instructions
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- JDK 17+
- Android SDK (API 34+)
- Docker & Docker Compose
- Firebase project (for auth)

### Local Development

```bash
# Start local PostgreSQL
cd docker
docker compose up -d db
cd ..

# Backend
cd backend
dotnet restore Convy.slnx
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<local PostgreSQL connection string>"
dotnet build Convy.slnx
dotnet run --project src/Convy.API --launch-profile http

# Mobile (Android)
cd ../mobile
./gradlew :shared:testDebugUnitTest
./gradlew :composeApp:testDebugUnitTest
./gradlew :androidApp:assembleLocalDebug
```

## Governance for AI-Assisted Development

This repo is structured for governed AI-assisted development: agents can accelerate delivery while explicit rules keep the codebase clean, testable, and aligned with SOLID principles.

### How It Works

| Primitive | Purpose |
|-----------|---------|
| `AGENTS.md` / `CLAUDE.md` | Global coding standards, always loaded |
| `.github/instructions/` | Auto-loaded rules per file type / layer |
| `.github/agents/` | Specialized AI personas (backend-dev, mobile-dev, reviewer, etc.) |
| `.github/skills/` | Step-by-step workflows (create feature, add screen, run review, etc.) |
| `.claude/skills/` | Claude Code copy of workflow skills |
| `.agents/skills/` | Codex copy of workflow skills |
| `.github/prompts/` | One-shot task templates (`/new-feature`, `/fix-bug`, etc.) |
| `.github/hooks/` | Deterministic guardrails (layer dependency validation) |

### Quick Usage

**Create a backend feature:**
```
Using the backend-feature skill, add household invitation support with code generation and expiration.
```

**Design a screen via Stitch:**
```
Using the design-screen skill, design a list detail screen with pending items, a FAB, and a collapsed completed section.
```

**Review code quality:**
```
Using the code-review skill, review the latest changes in Convy.Application for SOLID and architecture issues.
```

See [docs/USAGE.md](docs/USAGE.md) for the full guide with examples.

## Documentation

- [MVP Specification](docs/mvp-spec.md) — Full product spec
- [Architecture](docs/ARCHITECTURE.md) — System design and decisions
- [Testing Strategy](docs/TESTING.md) — Test conventions and commands
- [MCP Setup](docs/MCP-SETUP.md) — AI tooling configuration
- [ADRs](docs/adr/) — Architecture Decision Records

## Git Workflow

- **Branch naming:** `feature/xxx`, `fix/xxx`, `chore/xxx`
- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`)
- **PRs:** Feature branches → `main` via pull request

## License

Private — Not for distribution.
