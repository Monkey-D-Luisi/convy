# Convy — Copilot Global Instructions

## Orchestration Rules

When implementing any feature in Convy, **all affected layers must be covered in a single pass**:

1. **Domain** — Entities, value objects, repository interfaces
2. **Application** — Commands/Queries, Handlers, Validators, DTOs
3. **Infrastructure** — EF Core config, repository implementations, migrations
4. **API** — Endpoints, MediatR wiring, HTTP response mapping
5. **Mobile** — MVI contract, Store, Composable screen, navigation, DI
6. **Tests** — Unit (domain, application), integration (infrastructure, API)

Never implement only the backend and skip mobile, or vice versa. A feature is not complete until all relevant layers are done.

## Skill Loading

**Before implementing any layer, read its corresponding SKILL.md file first:**

| Layer | Skill to read |
|-------|--------------|
| Full backend feature | `.github/skills/backend-feature/SKILL.md` |
| Single API endpoint | `.github/skills/api-endpoint/SKILL.md` |
| Database migration | `.github/skills/db-migration/SKILL.md` |
| Mobile screen | `.github/skills/mobile-screen/SKILL.md` |
| UI design | `.github/skills/design-screen/SKILL.md` |
| Tests | `.github/skills/test-suite/SKILL.md` |
| Code review | `.github/skills/code-review/SKILL.md` |
| Firebase setup | `.github/skills/firebase-setup/SKILL.md` |

Skills contain tested procedures — do not skip steps or improvise alternatives.

## MCP Tool Usage

Use the configured MCP servers during implementation:

- **Context7** — Look up library documentation when unsure about APIs (MediatR, FluentValidation, EF Core, Ktor, Koin, Compose).
- **PostgreSQL** — Verify schema after migrations, inspect tables, run diagnostic queries.
- **GitHub** — Create branches, manage issues, create PRs when the feature is complete.
- **Stitch** — Generate UI mockups for any new screen before implementing the mobile layer.

## User Interaction via Questionnaires

When the agent needs information or action from the user, **always use `vscode_askQuestions`** instead of asking in plain text. This applies to:

- Confirming environment setup (Firebase project ID, package names, config file placement)
- Choosing between alternatives (e.g., change code vs. change Firebase config)
- Requesting manual steps the agent cannot perform (console setup, key generation)
- Validating generated designs or outputs before proceeding

Provide clear options with descriptions when the decisions are constrained. Use freeform input only when the answer is truly open-ended.

## Design-First for Mobile

When a feature includes a new mobile screen:

1. Generate the design in Stitch **before** writing Kotlin code.
2. **Always use `GEMINI_3_1_PRO`** model — never FLASH or default.
3. Follow the design-screen skill for the Stitch prompt structure — prompts must be detailed and comprehensive (all sections in the template are mandatory).
4. **Always generate a dark mode variant** after the light mode design.
5. **Ask the user** via `vscode_askQuestions` to confirm the design before implementing.
6. If Stitch is unavailable, document the design as a markdown spec in the PR description.

## Firebase Checkpoint

When a feature involves authentication or user identity:

1. Read `.github/skills/firebase-setup/SKILL.md`.
2. **Never assume Firebase is configured.** Run the verification checklist first.
3. Use `vscode_askQuestions` to gather Firebase project details from the user.
4. Verify `google-services.json` exists and has matching project/package values.
5. Verify backend `appsettings.json` has the correct `Firebase:ProjectId`.
6. Do not proceed with auth features until all Firebase checks pass.

## Build Verification

Every feature implementation must end with successful builds:

```bash
# Backend
dotnet build backend/Convy.slnx
dotnet test backend/Convy.slnx

# Mobile
cd mobile && ./gradlew :composeApp:assembleDebug
```

Do not mark a feature as complete if any build or test fails.
