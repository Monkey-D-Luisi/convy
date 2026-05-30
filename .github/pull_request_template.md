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
- [ ] Worker changes include registration tests and Docker/Compose validation
- [ ] MCP checks pass when MCP code changed: `cd mcp && npm test && npm run lint && npm run build`
- [ ] Mobile checks pass when mobile code changed: `cd mobile && ./gradlew :shared:testDebugUnitTest :composeApp:testDebugUnitTest :androidApp:assembleLocalDebug`
- [ ] Database changes include migration, rollback consideration, and orphan preflight when adding FKs
- [ ] Security-sensitive changes update `docs/SECURITY.md` and relevant runbooks
- [ ] No new warnings
- [ ] Architecture layers respected (Domain has no infra dependencies)
