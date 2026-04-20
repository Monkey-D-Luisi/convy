# Convy

> Coordinate your home in seconds, with minimal friction.

**Convy** is a shared mobile app for households — fast lists, tasks, and errands with real-time sync.

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
├── .github/              # Copilot governance + GitHub config
│   ├── instructions/     # File-specific coding instructions
│   ├── agents/           # Custom AI agents
│   ├── skills/           # Agent workflow skills
│   ├── prompts/          # Reusable prompt templates
│   ├── hooks/            # Lifecycle hooks
│   └── workflows/        # GitHub Actions CI/CD
├── .claude/              # Claude Code governance (mirrors .github/)
├── backend/              # ASP.NET Core solution (Clean Architecture)
│   ├── src/
│   │   ├── Convy.Domain/
│   │   ├── Convy.Application/
│   │   ├── Convy.Infrastructure/
│   │   └── Convy.API/
│   └── tests/
├── mobile/               # KMP Compose Multiplatform
│   ├── composeApp/       # Shared Compose UI
│   ├── shared/           # Shared business logic
│   └── androidApp/       # Android entry point
├── docs/                 # Specs, ADRs, guides
├── docker/               # Dockerfiles
├── AGENTS.md             # Cross-editor workspace instructions
├── CLAUDE.md             # Claude Code project instructions
└── docker-compose.yml    # Local development environment
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
# Start infrastructure (Postgres + pgAdmin)
docker-compose up -d

# Backend
cd backend
dotnet restore
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<local PostgreSQL connection string>"
dotnet build
dotnet run --project src/Convy.API

# Mobile (Android)
cd mobile
./gradlew :composeApp:assembleDebug
```

## Governance for AI-Assisted Development

This repo is structured for **vibe coding** — AI agents drive development while governance ensures clean, professional, SOLID code.

### How It Works

| Primitive | Purpose |
|-----------|---------|
| `AGENTS.md` / `CLAUDE.md` | Global coding standards, always loaded |
| `.github/instructions/` | Auto-loaded rules per file type / layer |
| `.github/agents/` | Specialized AI personas (backend-dev, mobile-dev, reviewer, etc.) |
| `.github/skills/` | Step-by-step workflows (create feature, add screen, run review, etc.) |
| `.github/prompts/` | One-shot task templates (`/new-feature`, `/fix-bug`, etc.) |
| `.github/hooks/` | Deterministic guardrails (layer dependency validation) |

### Quick Usage

**Create a backend feature:**
```
/backend-feature
> Add household invitation system with code generation and expiration
```

**Design a screen via Stitch:**
```
/design-screen
> Shopping list detail screen — items with checkboxes, FAB to add, completed section collapsed
```

**Review code quality:**
```
/code-review
> Review the latest changes in Convy.Application for SOLID violations
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
