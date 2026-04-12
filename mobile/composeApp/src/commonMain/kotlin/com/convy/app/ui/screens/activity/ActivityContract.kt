package com.convy.app.ui.screens.activity

import com.convy.shared.domain.model.ActivityLogEntry
import com.convy.app.util.UiText

data class ActivityState(
    val householdId: String = "",
    val groupedEntries: List<DateGroup> = emptyList(),
    val isLoading: Boolean = false,
    val isLoadingMore: Boolean = false,
    val hasMore: Boolean = true,
    val error: UiText? = null,
)

data class DateGroup(
    val date: String,
    val entries: List<ActivityLogEntry>,
)

sealed interface ActivityIntent {
    data object Refresh : ActivityIntent
    data object LoadMore : ActivityIntent
    data object NavigateBack : ActivityIntent
}

sealed interface ActivitySideEffect {
    data object NavigateBack : ActivitySideEffect
    data class ShowError(val message: String) : ActivitySideEffect
}
