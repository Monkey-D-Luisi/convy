# ADR-003: KMP + Compose Multiplatform for Mobile

## Status
Accepted

## Context
We need a mobile framework that supports Android first with future iOS expansion, shared business logic, and modern declarative UI.

## Decision
Use Kotlin Multiplatform (KMP) with Compose Multiplatform:
- **shared/** module for business logic (data, domain, DI)
- **composeApp/** module for shared UI with Compose Multiplatform
- **androidApp/** as the Android entry point
- MVI architecture (State/Intent/SideEffect/Store)

## Consequences
- Business logic is shared across platforms from day one
- Compose Multiplatform enables shared UI code
- Android-first approach reduces initial complexity
- iOS can be added later with minimal platform-specific code
- KMP ecosystem is maturing — some libraries may need platform-specific implementations
