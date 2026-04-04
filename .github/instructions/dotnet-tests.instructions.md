---
description: "Use when writing or editing tests for the backend — unit tests, integration tests, test fixtures, and test utilities."
applyTo: "backend/tests/**"
---
# Backend Testing Guidelines

## Framework
- **xUnit** for all tests.
- **FluentAssertions** for assertions.
- **NSubstitute** for mocking.
- **Testcontainers** for integration tests (real PostgreSQL).
- **WebApplicationFactory** for API integration tests.

## Test Naming
```
MethodName_Scenario_ExpectedResult
```
Examples:
- `CreateItem_WithValidData_ReturnsSuccess`
- `CreateItem_WithEmptyTitle_ReturnsValidationError`
- `CompleteItem_WhenAlreadyCompleted_ReturnsDomainError`

## Arrange-Act-Assert (AAA)
```csharp
[Fact]
public async Task CreateItem_WithValidData_ReturnsSuccess()
{
    // Arrange
    var command = new CreateItemCommand(listId, "Milk", null, 2, "liters");

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeEmpty();
}
```

## Test Structure

### Unit Tests (`Convy.Domain.Tests`, `Convy.Application.Tests`)
- Test domain entities, value objects, and handlers in isolation.
- Mock repository interfaces with NSubstitute.
- No database, no HTTP, no I/O.

### Integration Tests (`Convy.Infrastructure.Tests`)
- Test EF Core configurations, repositories against real PostgreSQL via Testcontainers.
- Use a shared `PostgresFixture` for container lifecycle.

### API Tests (`Convy.API.Tests`)
- Test full HTTP pipeline via `WebApplicationFactory<Program>`.
- Test auth, routing, serialization, error handling.
- Use Testcontainers for the database.

## Coverage Priorities
1. Domain entity invariants and business logic.
2. Command/query handlers (happy path + error cases).
3. Validators (boundary conditions).
4. API endpoints (auth, routing, error responses).
