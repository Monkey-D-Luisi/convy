package com.convy.app.ui.screens.listdetail

import com.convy.shared.domain.model.ListItem

data class ListDetailState(
    val listId: String = "",
    val householdId: String = "",
    val listName: String = "",
    val pendingItems: List<ListItem> = emptyList(),
    val completedItems: List<ListItem> = emptyList(),
    val showCompleted: Boolean = false,
    val isLoading: Boolean = false,
    val error: String? = null,
)

sealed interface ListDetailIntent {
    data object Refresh : ListDetailIntent
    data class ToggleItem(val itemId: String, val isCompleted: Boolean) : ListDetailIntent
    data class OpenItem(val itemId: String) : ListDetailIntent
    data object AddItem : ListDetailIntent
    data object ToggleCompletedVisibility : ListDetailIntent
    data object NavigateBack : ListDetailIntent
}

sealed interface ListDetailSideEffect {
    data class NavigateToCreateItem(val householdId: String, val listId: String) : ListDetailSideEffect
    data class NavigateToEditItem(val householdId: String, val listId: String, val itemId: String) : ListDetailSideEffect
    data object NavigateBack : ListDetailSideEffect
    data class ShowError(val message: String) : ListDetailSideEffect
}
