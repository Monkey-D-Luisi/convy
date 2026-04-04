---
description: "Add tests to existing code. Analyze the code, identify test gaps, and generate comprehensive test coverage."
agent: "agent"
---
# Add Tests

Add test coverage to existing Convy code.

## Input
Specify the file, class, or feature you want to add tests for.

## Process
1. **Analyze**: Read the target code to understand behavior and dependencies.
2. **Identify gaps**: Check existing tests for what's already covered.
3. **Plan scenarios**: List happy path, error cases, edge cases, and boundary conditions.
4. **Write tests**: Follow the `/test-suite` skill procedures.
5. **Run**: Execute tests and verify they all pass.

## Output
- New test files or additions to existing test files
- All tests pass: `dotnet test` / `./gradlew allTests`
