---
description: "Use when writing or editing Kotlin code for the mobile app — Compose UI, MVI stores, DI modules, networking, and shared logic."
applyTo: "mobile/**/*.kt"
---
# Kotlin / Compose Multiplatform Guidelines

## MVI Pattern
- Every screen has: `{Screen}State`, `{Screen}Intent`, `{Screen}SideEffect`, `{Screen}Store`.
- State is a `data class`, always immutable.
- Intent and SideEffect are `sealed interface`.
- Store exposes `state: StateFlow<State>` and `fun processIntent(intent: Intent)`.

## Compose Rules
- **State hoisting**: Composables receive state as parameters and emit intents via callbacks.
- **No business logic in composables**: Only render state, forward user actions.
- **Screen/Content split**: `{Screen}Screen` wires the Store. `{Screen}Content` is pure, previewable UI.
- **Preview**: Every content composable has a `@Preview` function with realistic sample data.
- **Theme**: Always use `ConvyTheme` and Material 3 tokens — no hardcoded colors/sizes.

## Networking (Ktor)
- DTOs are `@Serializable` data classes in `shared/data/remote/dto/`.
- Map DTOs → domain models at repository boundary.
- Handle network errors in the repository, return `Result<T>`.
- Attach Firebase token via Ktor auth plugin.

## Dependency Injection (Koin)
- Feature-scoped modules: `authModule`, `householdModule`, `listModule`.
- Platform modules in `androidApp/` for platform-specific implementations.
- Constructor injection only — no `get()` calls in production code.

## Kotlin Style
- `sealed interface` over `sealed class` for Intent/SideEffect.
- `data object` for parameterless sealed members.
- `val` over `var` wherever possible.
- Explicit return types on public functions.
- Use `kotlinx.serialization` — no Gson, no reflection-based serializers.
- Structured concurrency with `CoroutineScope` — no `GlobalScope`.
