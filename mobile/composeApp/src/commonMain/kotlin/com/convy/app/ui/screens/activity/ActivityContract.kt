package com.convy.app.ui.screens.activity

import com.convy.shared.domain.model.ActivityLogEntry

data class ActivityState(
    val householdId: String = "",
    val entries: List<ActivityLogEntry> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null,
)

sealed interface ActivityIntent {
    data object Refresh : ActivityIntent
    data object NavigateBack : ActivityIntent
}

sealed interface ActivitySideEffect {
    data object NavigateBack : ActivitySideEffect
    data class ShowError(val message: String) : ActivitySideEffect
}
