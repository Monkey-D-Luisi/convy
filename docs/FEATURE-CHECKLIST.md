# Feature Implementation Checklist

Universal checklist for implementing any feature in Convy. Use this whether implementing manually or via AI agents.

## Before Starting

- [ ] Read the user story / spec in `docs/mvp-spec.md`
- [ ] Identify scope: backend only, mobile only, or full vertical
- [ ] Check if the feature involves authentication → read `.github/skills/firebase-setup/SKILL.md`

## Design (if feature includes a new screen)

- [ ] Read `.github/skills/design-screen/SKILL.md`
- [ ] Generate screen design in Stitch (`mcp_stitch_generate_screen_from_text`)
- [ ] Generate dark mode variant (`mcp_stitch_generate_variants`)
- [ ] Design covers all states: default, empty, loading, error
- [ ] If Stitch unavailable → document design as markdown spec

## Backend — Domain Layer

- [ ] Read `.github/skills/backend-feature/SKILL.md` (Step 1)
- [ ] Entity created/updated with invariants (guard clauses, private setters)
- [ ] Value objects defined if new concepts introduced
- [ ] Repository interface added in `Domain/Repositories/`
- [ ] Domain events added if cross-aggregate side effects needed
- [ ] No external dependencies in Domain layer

## Backend — Application Layer

- [ ] Read `.github/skills/backend-feature/SKILL.md` (Step 2)
- [ ] Command or Query record created in `Features/{Feature}/`
- [ ] Handler implements `IRequestHandler<TRequest, TResponse>`
- [ ] FluentValidation validator covers all input constraints
- [ ] DTOs created for API response mapping
- [ ] Result pattern used (no exceptions for business logic)

## Backend — Infrastructure Layer

- [ ] Read `.github/skills/backend-feature/SKILL.md` (Step 3)
- [ ] EF Core entity configuration in `Persistence/Configurations/`
- [ ] Repository implementation from Domain interface
- [ ] Migration created: `dotnet ef migrations add <Name> --project src/Convy.Infrastructure --startup-project src/Convy.API`
- [ ] Migration has both `Up()` and `Down()` methods
- [ ] Verify schema with PostgreSQL MCP if Docker is running

## Backend — API Layer

- [ ] Read `.github/skills/backend-feature/SKILL.md` (Step 4)
- [ ] Minimal API endpoint defined
- [ ] `IMediator.Send()` wired
- [ ] HTTP responses mapped: 200/201/204/400/404/500
- [ ] `RequireAuthorization()` applied
- [ ] Endpoint registered in `Program.cs`

## Mobile — Implementation

- [ ] Read `.github/skills/mobile-screen/SKILL.md`
- [ ] MVI contract: State, Intent, SideEffect defined
- [ ] Store handles all intents, updates state immutably
- [ ] Screen composable split into Screen (wiring) + Content (pure UI)
- [ ] All states rendered: loading, empty, data, error
- [ ] Navigation route and arguments wired
- [ ] DI: Store registered in Koin module
- [ ] Previews with realistic sample data for all key states

## Tests

- [ ] Read `.github/skills/test-suite/SKILL.md`
- [ ] Domain unit tests: entity behavior, invariants
- [ ] Application unit tests: handler logic with mocked repos
- [ ] Infrastructure integration tests: repository with Testcontainers
- [ ] API integration tests: WebApplicationFactory endpoint tests
- [ ] Tests cover happy path + error cases + edge cases

## Verification

- [ ] Backend builds: `dotnet build backend/Convy.slnx`
- [ ] Backend tests pass: `dotnet test backend/Convy.slnx`
- [ ] Mobile builds: `cd mobile && ./gradlew :composeApp:assembleDebug`
- [ ] Quick code review via `.github/skills/code-review/SKILL.md`
- [ ] No layer dependency violations (Domain must not import Infrastructure/EF)

## Completion

- [ ] All checklist items above are done (for applicable layers)
- [ ] Feature works end-to-end: API endpoint → mobile UI
- [ ] Code follows naming conventions from `AGENTS.md`
- [ ] Conventional commit message prepared
