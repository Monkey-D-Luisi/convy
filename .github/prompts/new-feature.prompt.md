---
description: "Orchestrate the creation of a full vertical feature across backend and mobile. Reads skills, uses MCPs, coordinates all layers."
mode: "agent"
tools: ["read", "edit", "search", "execute", "stitch/*"]
---
# New Feature — Full Vertical Implementation

Implement a complete feature for the Convy app spanning **all layers**: Domain → Application → Infrastructure → API → Mobile UI → Tests.

## Input
- `$input` — Feature description. Can be a user story number (e.g., "9.3"), a spec section reference, or a free-text description.

## Phase 0 — Analysis & Planning
1. Read `docs/mvp-spec.md` to extract the exact requirements for this feature.
2. Read `docs/ARCHITECTURE.md` to confirm the architectural patterns.
3. Identify the scope: which layers are affected (backend, mobile, or both).
4. If the feature involves **authentication**, read `.github/skills/firebase-setup/SKILL.md` and verify Firebase is configured.
5. List the concrete deliverables (entities, commands/queries, endpoints, screens).

## Phase 1 — UI Design (if feature has a screen)
1. Read `.github/skills/design-screen/SKILL.md` for the Stitch workflow.
2. Compose the Stitch prompt following the template in the skill.
3. Generate the design via `mcp_stitch_generate_screen_from_text` (create project first with `mcp_stitch_create_project` if needed).
4. Generate dark mode and state variants via `mcp_stitch_generate_variants`.
5. If Stitch is unavailable, document the screen design as a markdown spec with layout, components, states, and interactions.

## Phase 2 — Backend Implementation
1. **Read** `.github/skills/backend-feature/SKILL.md` — follow it step by step.
2. **Domain** (`backend/src/Convy.Domain/`): Entities with invariants, value objects, repository interfaces.
3. **Application** (`backend/src/Convy.Application/`): Command/Query, Handler, FluentValidation Validator, DTOs.
4. **Infrastructure** (`backend/src/Convy.Infrastructure/`): EF Core config, repository impl, migration.
5. **API** (`backend/src/Convy.API/`): Minimal API endpoint, MediatR wiring, HTTP response mapping.
6. Use the **Context7 MCP** (`context7`) to look up library docs if unsure about an API (e.g., MediatR, FluentValidation, EF Core).
7. Use the **PostgreSQL MCP** (`postgres`) to verify schema after migration if Docker is running.

## Phase 3 — Mobile Implementation (if feature has UI)
1. **Read** `.github/skills/mobile-screen/SKILL.md` — follow it step by step.
2. **Contract**: State, Intent, SideEffect in `mobile/shared/src/commonMain/`.
3. **Store**: MVI store with repository injection.
4. **Screen**: Composable split into Screen (wiring) + Content (pure UI).
5. **Navigation**: Wire route and arguments.
6. **DI**: Register in Koin module.
7. **Previews**: Create `@Preview` for all states.

## Phase 4 — Tests
1. **Read** `.github/skills/test-suite/SKILL.md` for test patterns.
2. **Domain unit tests**: Entity behavior, invariants (`Convy.Domain.Tests`).
3. **Application unit tests**: Handler logic with mocked repos (`Convy.Application.Tests`).
4. **Infrastructure integration tests**: Repository with Testcontainers (`Convy.Infrastructure.Tests`).
5. **API integration tests**: WebApplicationFactory endpoint tests (`Convy.API.Tests`).

## Phase 5 — Verification
1. Build backend: `dotnet build backend/Convy.slnx`
2. Run backend tests: `dotnet test backend/Convy.slnx`
3. Build mobile: `cd mobile && ./gradlew :composeApp:assembleDebug`
4. Run a quick code review following `.github/skills/code-review/SKILL.md` checklist.

## Output
- Working backend endpoint(s) with full test coverage
- Mobile screen with all states and previews (if applicable)
- Database migration (if schema changes)
- All code compiles and all tests pass
- Design artifacts in Stitch (or markdown spec as fallback)
