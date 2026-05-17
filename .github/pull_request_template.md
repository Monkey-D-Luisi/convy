## Description

<!-- Brief description of what this PR does -->

## Type of Change

- [ ] feat: New feature
- [ ] fix: Bug fix
- [ ] refactor: Code refactoring
- [ ] docs: Documentation
- [ ] test: Tests
- [ ] chore: Maintenance

## Checklist

- [ ] Code follows project conventions (see AGENTS.md)
- [ ] Tests added/updated
- [ ] Backend build passes: `dotnet build backend/Convy.slnx`
- [ ] Backend tests pass: `dotnet test backend/Convy.slnx`
- [ ] Mobile checks pass when mobile code changed: `cd mobile && ./gradlew :shared:testDebugUnitTest :composeApp:testDebugUnitTest :androidApp:assembleLocalDebug`
- [ ] No new warnings
- [ ] Architecture layers respected (Domain has no infra dependencies)
