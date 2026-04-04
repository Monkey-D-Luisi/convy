---
name: code-review
description: "Structured code review workflow. Use when reviewing code changes for SOLID violations, security issues, architecture compliance, performance problems, and code smells."
---
# Code Review Workflow

## When to Use
- Reviewing a feature branch or PR
- Auditing existing code for quality issues
- Pre-merge quality gate

## Procedure

### Step 1: Identify Scope
Determine what to review:
- Specific files/folders
- A feature branch (diff)
- A full module/layer

### Step 2: Architecture Review
Check layer dependency compliance:
```
Domain → ZERO external dependencies
Application → depends on Domain only
Infrastructure → depends on Application + Domain
API → depends on all (DI wiring)
```

Verify:
- [ ] No Infrastructure imports in Domain or Application
- [ ] No EF Core types in Domain
- [ ] No business logic in API controllers/endpoints
- [ ] DTOs at API boundary, never domain entities

### Step 3: SOLID Review
- **S** — Single Responsibility: Does each class do one thing?
- **O** — Open/Closed: Can behavior be extended without modifying existing code?
- **L** — Liskov Substitution: Are subtypes properly substitutable?
- **I** — Interface Segregation: Are interfaces focused?
- **D** — Dependency Inversion: Do high-level modules depend on abstractions?

### Step 4: Security Review (OWASP Top 10)
- [ ] No secrets in source code
- [ ] Input validation at boundaries
- [ ] Parameterized queries (no string concatenation for SQL)
- [ ] Authentication required on all endpoints
- [ ] No mass assignment (binding directly to entities)
- [ ] No sensitive data in logs
- [ ] Proper error handling (no stack traces to clients)

### Step 5: Code Quality
- [ ] No dead code or TODOs without tickets
- [ ] Meaningful naming
- [ ] No premature abstractions
- [ ] Async all the way (no `.Result`, `.Wait()`)
- [ ] Proper disposal of resources
- [ ] Consistent error handling pattern (Result<T>)

### Step 6: Test Coverage
- [ ] Critical business logic has unit tests
- [ ] Edge cases covered
- [ ] Tests are deterministic and independent

### Step 7: Report
Format findings as:
```
## Review Summary
Files reviewed: N | Issues: X critical, Y major, Z minor

### [Critical] Issue title
**File:** path/to/file (line N)
**Issue:** What's wrong
**Fix:** How to fix it
```

## Severity Guide
| Level | Definition |
|-------|-----------|
| Critical | Security vulnerability, data loss risk, broken architecture |
| Major | SOLID violation, missing validation, untested critical path |
| Minor | Naming issue, style inconsistency, minor optimization |
| Suggestion | Nice-to-have improvement, not blocking |
