# Mobile — KMP + Compose Multiplatform Guidelines

## Architecture: MVI (Model-View-Intent)

### Pattern

```
User Action → Intent → Store/ViewModel → State → Composable UI
                            ↓
                        Side Effects (navigation, API calls, toasts)
```

### Components

- **State**: Immutable data class representing the full UI state of a screen.
- **Intent**: Sealed interface representing user actions and system events.
- **SideEffect**: Sealed interface for one-time events (navigation, snackbars).
- **Store**: Processes intents, updates state, emits side effects. Uses `StateFlow` for state and `Channel`/`SharedFlow` for side effects.

### Naming Convention

| Component | Pattern | Example |
|-----------|---------|---------|
| State | `{Screen}State` | `ListDetailState` |
| Intent | `{Screen}Intent` | `ListDetailIntent` |
| Side Effect | `{Screen}SideEffect` | `ListDetailSideEffect` |
| Store | `{Screen}Store` | `ListDetailStore` |
| Screen composable | `{Screen}Screen` | `ListDetailScreen` |
| Content composable | `{Screen}Content` | `ListDetailContent` |

### Example Structure

```kotlin
// State
data class ListDetailState(
    val items: List<ListItemUi> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null
)

// Intent
sealed interface ListDetailIntent {
    data class ToggleItem(val itemId: String) : ListDetailIntent
    data class DeleteItem(val itemId: String) : ListDetailIntent
    data object Refresh : ListDetailIntent
}

// Side Effect
sealed interface ListDetailSideEffect {
    data class ShowError(val message: String) : ListDetailSideEffect
    data object NavigateBack : ListDetailSideEffect
}
```

## Project Structure

```
mobile/
├── composeApp/           # Shared Compose UI (screens, components, theme, navigation)
├── shared/               # Shared business logic
│   ├── domain/           # Entities, use case interfaces, repository interfaces
│   ├── data/             # Repository implementations, Ktor client, DTOs, mappers
│   └── di/               # Koin modules
└── androidApp/           # Android entry point, manifest, platform DI
```

## Compose Multiplatform Conventions

- **State hoisting**: Composables receive state and emit events, never hold mutable state internally.
- **Preview**: Every screen has a `@Preview` composable with sample data.
- **Theme**: Use Material 3 with a centralized `ConvyTheme`.
- **No business logic in composables**: Composables only render state and forward intents.
- **Screen vs Content**: `{Screen}Screen` handles Store wiring + side effects. `{Screen}Content` is pure UI.

```kotlin
@Composable
fun ListDetailScreen(store: ListDetailStore) {
    val state by store.state.collectAsState()
    // Collect side effects, handle navigation
    ListDetailContent(state = state, onIntent = store::processIntent)
}

@Composable
fun ListDetailContent(state: ListDetailState, onIntent: (ListDetailIntent) -> Unit) {
    // Pure UI rendering
}
```

## Dependency Injection (Koin)

- Modules per feature: `authModule`, `householdModule`, `listModule`.
- Platform-specific modules in `androidApp/` (and future `iosApp/`).
- Use constructor injection — no service locator pattern in business logic.

## Networking (Ktor)

- Single `HttpClient` configured in DI.
- Request/response DTOs in `shared/data/`.
- Map DTOs to domain models at the repository boundary.
- Auth: Attach Firebase ID token via Ktor plugin/interceptor.

## Kotlin Conventions

- `sealed interface` over `sealed class` for Intent/SideEffect.
- `data class` for State and DTOs.
- `data object` for parameterless intents.
- Prefer `Result<T>` for error handling over exceptions.
- Coroutines: structured concurrency, cancel-safe operations.
- Use `kotlinx.serialization` for JSON — no reflection-based serializers.
