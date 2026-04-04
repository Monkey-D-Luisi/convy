---
name: mobile-screen
description: "Workflow for creating a new screen in the Convy mobile app. Use when adding a new screen or view with Compose Multiplatform and MVI architecture. Covers state modeling, store creation, composable UI, navigation wiring, and previews."
---
# Mobile Screen Workflow

## When to Use
- Adding a new screen to the mobile app
- Implementing a user story that requires a new UI view

## Prerequisites
- Read `docs/mvp-spec.md` for screen specification (Section 12).
- Read `mobile/AGENTS.md` for MVI and Compose conventions.
- Check if a design exists in Stitch for this screen.

## Procedure

### Step 1: Define MVI Contract (`mobile/shared/domain/`)
1. Create `{Screen}State` data class with all UI fields and initial defaults.
2. Create `{Screen}Intent` sealed interface with all user actions.
3. Create `{Screen}SideEffect` sealed interface for one-time events.

```kotlin
// Example
data class ListDetailState(
    val items: List<ListItemUi> = emptyList(),
    val isLoading: Boolean = false
)

sealed interface ListDetailIntent {
    data class ToggleItem(val id: String) : ListDetailIntent
    data object Refresh : ListDetailIntent
}

sealed interface ListDetailSideEffect {
    data class ShowError(val message: String) : ListDetailSideEffect
}
```

### Step 2: Create Store (`mobile/shared/domain/`)
1. Create `{Screen}Store` class with:
   - `state: StateFlow<{Screen}State>`
   - `sideEffects: SharedFlow<{Screen}SideEffect>`
   - `fun processIntent(intent: {Screen}Intent)`
2. Inject repository dependencies via constructor.
3. Handle each intent case and update state immutably.

### Step 3: Build UI (`mobile/composeApp/`)
1. Create `{Screen}Screen` composable — wires Store, collects state and side effects.
2. Create `{Screen}Content` composable — pure UI, receives state + `onIntent` callback.
3. Use Material 3 components from `ConvyTheme`.
4. Handle all states: loading, empty, data, error.

### Step 4: Navigation
1. Add screen route to the navigation graph.
2. Wire arguments if needed (e.g., list ID).
3. Handle side effects for navigation (back, forward).

### Step 5: DI
1. Register Store in the feature's Koin module.
2. Register any new repository if needed.

### Step 6: Previews
1. Create `@Preview` for `{Screen}Content` with sample data.
2. Create previews for key states: default, empty, loading, error.

### Step 7: Verify
```bash
cd mobile
./gradlew :composeApp:assembleDebug
```

## Checklist
- [ ] State, Intent, SideEffect defined
- [ ] Store handles all intents
- [ ] Screen/Content split (wiring vs pure UI)
- [ ] State hoisting — no mutable state in composables
- [ ] All states handled (loading, empty, data, error)
- [ ] Previews with realistic sample data
- [ ] Koin module updated
- [ ] Navigation wired
- [ ] Uses ConvyTheme — no hardcoded styles
