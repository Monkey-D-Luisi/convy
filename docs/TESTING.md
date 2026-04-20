# Convy — Testing Strategy

## Backend

### Test Projects

| Project | Tests | Frameworks |
|---------|-------|------------|
| Convy.Domain.Tests | Unit tests for entities, value objects | xUnit, FluentAssertions |
| Convy.Application.Tests | Unit tests for handlers, validators | xUnit, FluentAssertions, NSubstitute |
| Convy.Infrastructure.Tests | Integration tests for repositories | xUnit, FluentAssertions, Testcontainers.PostgreSql |
| Convy.API.Tests | Integration tests for endpoints | xUnit, FluentAssertions, WebApplicationFactory |

### Running Tests

```bash
# All tests
dotnet test backend/Convy.slnx

# Specific project
dotnet test backend/tests/Convy.Domain.Tests

# With coverage
dotnet test backend/Convy.slnx --collect:"XPlat Code Coverage"
```

### Conventions

- Test class name: `{ClassUnderTest}Tests`
- Method name: `{Method}_Should{Expected}_When{Condition}`
- Arrange-Act-Assert pattern
- One assertion concept per test (multiple FluentAssertions on same object is OK)
- Use NSubstitute for mocking interfaces
- Testcontainers for real PostgreSQL in integration tests

## Mobile

### Frameworks

- `kotlin.test` — assertions
- `kotlinx.coroutines.test` — coroutine testing
- Turbine (add later) — Flow testing

### Running Tests

```bash
cd mobile
./gradlew :shared:testDebugUnitTest
./gradlew :composeApp:testDebugUnitTest
```

### Conventions

- Test class name: `{ClassUnderTest}Test`
- Method name: `` `should do X when Y` ``
- Use `runTest` for coroutine tests

## E2E Tests (Maestro)

End-to-end UI tests run on an Android emulator using [Maestro](https://maestro.mobile.dev/).

### Prerequisites

| Requirement | How to verify |
|---|---|
| Android emulator running | `adb devices` → shows `emulator-5554` |
| Backend API running on port 5062 | `dotnet run --project backend/src/Convy.API --launch-profile http` |
| PostgreSQL running via Docker | `docker-compose up -d` in `docker/` |
| Maestro CLI installed | `maestro --version` → 2.4.0+ |
| App installed on emulator | Build `local` flavor: `./gradlew :androidApp:assembleLocalDebug` then `adb install -r` |

The `local` build flavor points to `http://10.0.2.2:5062` (emulator's alias for host localhost).

### Running E2E Tests

```bash
# Recommended: use the wrapper script (generates unique emails per run)
cd mobile
powershell -File e2e/run-e2e.ps1

# Manual equivalent. Prefer the wrapper script because it reads APP_VERSION from Gradle.
maestro test -e EMAIL="e2e_<timestamp>@test.com" -e JOIN_EMAIL="e2e_join_<timestamp>@test.com" -e APP_VERSION="<current-versionName>" e2e/
```

### Suite Structure

- **`e2e/config.yaml`** — Suite configuration with `flows: ["*"]` and ordered execution.
- **`e2e/flow_*.yaml`** — 23 test flows, executed sequentially. Each flow depends on state created by previous flows.
- **`e2e/pending/`** — Flows for features not yet implemented (excluded from suite).
- **`e2e/run-e2e.ps1`** — PowerShell wrapper that generates unique timestamped emails.

### Flow Execution Order

Flows run sequentially and are **stateful** — each builds on the previous:

1. `flow_register` — Creates a user account (clears app state)
2. `flow_create_household` — Creates "Test Home" household
3. `flow_create_list` — Creates "Weekly Groceries" shopping list
4. `flow_create_tasks_list` — Creates "Home Tasks" task list
5. `flow_rename_list` — Renames list
6. `flow_archive_list` — Archives list
7. `flow_add_item` through `flow_filter_items` — Item CRUD, completion, filtering
8. `flow_shopping_mode` through `flow_sign_in` — Household management, auth flows

### Key Conventions

#### Environment Variables (not output variables)

Maestro output variables (`output.*`) do **not** persist between flows in a suite. Use CLI `-e` flags:

```bash
maestro test -e EMAIL="unique@test.com" -e JOIN_EMAIL="join@test.com" -e APP_VERSION="<current-versionName>" e2e/
```

Reference in flows as `${EMAIL}`, `${JOIN_EMAIL}`.

#### Flow File Format

Every flow must have an `appId` header separated by `---`:

```yaml
appId: com.monkeydluisi.convy
---
- launchApp:
    clearState: false
- extendedWaitUntil:
    visible:
      text: "Test Home"
    timeout: 15000
```

#### config.yaml Format

```yaml
flows:
  - "*"                    # Scans immediate directory only (excludes pending/)
executionOrder:
  continueOnFailure: true
  flowsOrder:
    - flow_register        # NO .yaml extension
    - flow_create_household
    # ...
```

- `flows: ["*"]` is required — prevents scanning subdirectories like `pending/`
- `flowsOrder` entries must **not** have `.yaml` extension
- Do **not** put `appId` in config.yaml — each flow declares its own

### Compose-Specific Gotchas

#### testTags Require `testTagsAsResourceId`

Compose `testTag("foo")` is **not** exposed as Android `resource-id` by default. The app has a root-level modifier in `App.kt`:

```kotlin
Box(Modifier.semantics { testTagsAsResourceId = true }) {
    ConvyTheme { /* ... */ }
}
```

Without this, `id: "item-checkbox"` selectors in Maestro won't match anything. New testTags work automatically thanks to this root modifier.

#### Collapsed Completed Section

`ListDetailState.showCompleted` defaults to `false`. Completed items are **not rendered** until the section is expanded:

- Section header "Completed (N)" is visible with an expand IconButton
- The IconButton has `contentDescription = "Show"` (collapsed) / `"Hide"` (expanded)
- To interact with completed items: wait for `text: "Show"` → tap `"Show"` → wait for checkbox

#### YAML Escaping — Avoid Parentheses

In YAML double-quoted strings, `\\(` produces a literal backslash `\(`, **not** the text `(`. Never use escaped parentheses to match UI text like "Completed (1)". Use alternative selectors (contentDescription, id, etc.).

### Debugging

```bash
# Run a single flow
maestro test -e EMAIL="test@test.com" e2e/flow_register.yaml

# View UI hierarchy (accessibility tree)
maestro hierarchy

# Add screenshot to a flow
- takeScreenshot: debug_screenshot
```

### Adding New Flows

1. Create `e2e/flow_<name>.yaml` with `appId` header
2. Add `flow_<name>` (no extension) to `config.yaml` → `flowsOrder` at the correct position
3. Consider what state previous flows leave and what state your flow leaves for subsequent flows
4. Use `extendedWaitUntil` with generous timeouts (10000–15000ms) instead of `assertVisible` for dynamic content
5. Use `id:` selectors (testTags) over `text:` when text might change or isn't unique
6. Test the flow individually first, then run the full suite
