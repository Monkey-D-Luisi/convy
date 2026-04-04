---
name: test-suite
description: "Workflow for generating a complete test suite for a feature. Use when adding comprehensive tests — unit, integration, and API tests for a backend feature, or Store and UI tests for mobile."
---
# Test Suite Workflow

## When to Use
- Adding tests for a new or existing feature
- Filling test coverage gaps
- Writing regression tests after a bug fix

## Procedure

### Step 1: Analyze the Feature
1. Read the implementation code to understand behavior.
2. Identify inputs, outputs, dependencies, and edge cases.
3. List all scenarios to test.

### Step 2: Plan Test Scenarios
For each unit under test, enumerate:
- **Happy path**: Normal successful operation
- **Validation failures**: Invalid inputs
- **Domain errors**: Business rule violations
- **Edge cases**: Empty collections, null optionals, boundary values
- **Concurrency**: Concurrent modifications (if applicable)

### Step 3: Write Backend Tests

#### Domain Tests (`Convy.Domain.Tests`)
```csharp
public class ListItemTests
{
    [Fact]
    public void Create_WithValidTitle_Succeeds() { }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsDomainException() { }

    [Fact]
    public void Complete_WhenPending_SetsCompletedState() { }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ThrowsDomainException() { }
}
```

#### Application Tests (`Convy.Application.Tests`)
- Mock repositories with NSubstitute.
- Test handler logic in isolation.
- Verify correct repository calls.
- Test validators separately.

#### Infrastructure Tests (`Convy.Infrastructure.Tests`)
- Use Testcontainers for real PostgreSQL.
- Test repository queries return correct data.
- Test EF Core configurations map correctly.

#### API Tests (`Convy.API.Tests`)
- Use WebApplicationFactory.
- Test HTTP status codes.
- Test auth requirements.
- Test request/response serialization.

### Step 4: Write Mobile Tests

#### Store Tests
- Test state transitions for each intent.
- Verify side effects are emitted correctly.
- Test error handling.

### Step 5: Run and Verify
```bash
# Backend
cd backend && dotnet test --verbosity normal

# Mobile
cd mobile && ./gradlew allTests
```

## Test Quality Checklist
- [ ] Each test tests ONE thing
- [ ] Test names describe the scenario
- [ ] No test depends on another test's state
- [ ] No hardcoded delays or Thread.Sleep
- [ ] Assertions are specific (not just "no exception thrown")
- [ ] Integration tests clean up after themselves
- [ ] All tests pass in CI
