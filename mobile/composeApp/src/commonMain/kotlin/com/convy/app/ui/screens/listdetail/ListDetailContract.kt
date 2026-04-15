package com.convy.app.ui.screens.listdetail

import com.convy.shared.domain.model.ListItem
import com.convy.app.util.UiText

data class ListDetailState(
    val listId: String = "",
    val householdId: String = "",
    val listName: String = "",
    val listType: String = "",
    val isShoppingMode: Boolean = false,
    val pendingItems: List<ListItem> = emptyList(),
    val completedItems: List<ListItem> = emptyList(),
    val showCompleted: Boolean = false,
    val isLoading: Boolean = false,
    val error: UiText? = null,
    val searchQuery: String = "",
    val isSearching: Boolean = false,
    val activeFilter: String = "All",
    val isRecording: Boolean = false,
    val isProcessingVoice: Boolean = false,
    val voiceTranscription: String = "",
    val parsedVoiceItems: List<ParsedVoiceItem> = emptyList(),
    val showVoiceSheet: Boolean = false,
    val pendingSyncCount: Int = 0,
)

data class ParsedVoiceItem(
    val title: String,
    val quantity: Int?,
    val unit: String?,
    val matchedExistingItem: String? = null,
    val isSelected: Boolean = true,
)

sealed interface ListDetailIntent {
    data object Refresh : ListDetailIntent
    data class ToggleItem(val itemId: String, val isCompleted: Boolean) : ListDetailIntent
    data class OpenItem(val itemId: String) : ListDetailIntent
    data object AddItem : ListDetailIntent
    data object ToggleCompletedVisibility : ListDetailIntent
    data object NavigateBack : ListDetailIntent
    data class DeleteItem(val itemId: String) : ListDetailIntent
    data class UpdateSearchQuery(val query: String) : ListDetailIntent
    data object ToggleSearch : ListDetailIntent
    data class SetFilter(val filter: String) : ListDetailIntent
    data object ToggleShoppingMode : ListDetailIntent
    data object StartRecording : ListDetailIntent
    data object StopRecording : ListDetailIntent
    data object DismissVoiceSheet : ListDetailIntent
    data class ToggleVoiceItem(val index: Int) : ListDetailIntent
    data object ConfirmVoiceItems : ListDetailIntent
}

sealed interface ListDetailSideEffect {
    data class NavigateToCreateItem(val householdId: String, val listId: String) : ListDetailSideEffect
    data class NavigateToEditItem(val householdId: String, val listId: String, val itemId: String) : ListDetailSideEffect
    data object NavigateBack : ListDetailSideEffect
    data class ShowError(val message: String) : ListDetailSideEffect
}
