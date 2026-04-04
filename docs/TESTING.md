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
./gradlew :shared:allTests
./gradlew :composeApp:allTests
```

### Conventions

- Test class name: `{ClassUnderTest}Test`
- Method name: `` `should do X when Y` ``
- Use `runTest` for coroutine tests
