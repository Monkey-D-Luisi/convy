package com.convy.app.ui.screens.listdetail

import com.convy.app.util.UiText
import com.convy.shared.domain.model.ListItem
import com.convy.shared.domain.model.TaskItem

data class ListDetailState(
    val listId: String = "",
    val householdId: String = "",
    val listName: String = "",
    val listType: String = "",
    val isShoppingMode: Boolean = false,
    val pendingEntries: List<ListEntryUi> = emptyList(),
    val completedEntries: List<ListEntryUi> = emptyList(),
    val completionExitEntryIds: Set<String> = emptySet(),
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

data class ListEntryUi(
    val id: String,
    val title: String,
    val note: String?,
    val listId: String,
    val createdByName: String,
    val createdAt: String,
    val isCompleted: Boolean,
    val completedByName: String?,
    val completedAt: String?,
    val quantity: Int? = null,
    val unit: String? = null,
    val recurrenceFrequency: String? = null,
)

data class ParsedVoiceItem(
    val title: String,
    val quantity: Int?,
    val unit: String?,
    val matchedExistingItem: String? = null,
    val isSelected: Boolean = true,
)

val ListDetailState.isTaskList: Boolean
    get() = listType.equals("Tasks", ignoreCase = true)

val ListDetailState.showNormalListChrome: Boolean
    get() = !isShoppingMode

data class ShoppingModeTransition(
    val state: ListDetailState,
    val shouldReloadItems: Boolean,
)

fun ListDetailState.toggleShoppingMode(): ShoppingModeTransition {
    if (isTaskList) {
        return ShoppingModeTransition(this, shouldReloadItems = false)
    }

    val enteringShoppingMode = !isShoppingMode
    val nextState = copy(
        isShoppingMode = enteringShoppingMode,
        isSearching = if (enteringShoppingMode) false else isSearching,
        searchQuery = if (enteringShoppingMode) "" else searchQuery,
        activeFilter = if (enteringShoppingMode) "All" else activeFilter,
    )

    return ShoppingModeTransition(
        state = nextState,
        shouldReloadItems = enteringShoppingMode && activeFilter != "All",
    )
}

fun List<ParsedVoiceItem>.toggleSelectionAt(index: Int): List<ParsedVoiceItem> =
    if (index !in indices) {
        this
    } else {
        mapIndexed { itemIndex, item ->
            if (itemIndex == index) item.copy(isSelected = !item.isSelected) else item
        }
    }

fun ListItem.toListEntryUi(): ListEntryUi = ListEntryUi(
    id = id,
    title = title,
    note = note,
    listId = listId,
    createdByName = createdByName,
    createdAt = createdAt,
    isCompleted = isCompleted,
    completedByName = completedByName,
    completedAt = completedAt,
    quantity = quantity,
    unit = unit,
    recurrenceFrequency = recurrenceFrequency,
)

fun TaskItem.toListEntryUi(): ListEntryUi = ListEntryUi(
    id = id,
    title = title,
    note = note,
    listId = listId,
    createdByName = createdByName,
    createdAt = createdAt,
    isCompleted = isCompleted,
    completedByName = completedByName,
    completedAt = completedAt,
)

sealed interface ListDetailIntent {
    data object Refresh : ListDetailIntent
    data class ToggleItem(val itemId: String, val isCompleted: Boolean) : ListDetailIntent
    data class OpenItem(val itemId: String) : ListDetailIntent
    data object AddItem : ListDetailIntent
    data object ToggleCompletedVisibility : ListDetailIntent
    data object NavigateBack : ListDetailIntent
    data class DeleteItem(val itemId: String) : ListDetailIntent
    data class UndoOperation(val operationId: Long) : ListDetailIntent
    data class RedoOperation(val operationId: Long) : ListDetailIntent
    data class CommitPendingDelete(val operationId: Long) : ListDetailIntent
    data class UpdateSearchQuery(val query: String) : ListDetailIntent
    data object ToggleSearch : ListDetailIntent
    data class SetFilter(val filter: String) : ListDetailIntent
    data object ToggleShoppingMode : ListDetailIntent
    data object StartRecording : ListDetailIntent
    data object StopRecording : ListDetailIntent
    data object VoicePermissionDenied : ListDetailIntent
    data object DismissVoiceSheet : ListDetailIntent
    data class ToggleVoiceItem(val index: Int) : ListDetailIntent
    data object ConfirmVoiceItems : ListDetailIntent
}

sealed interface ListDetailSideEffect {
    data class NavigateToCreateItem(val householdId: String, val listId: String) : ListDetailSideEffect
    data class NavigateToEditItem(val householdId: String, val listId: String, val itemId: String) : ListDetailSideEffect
    data class NavigateToCreateTask(val householdId: String, val listId: String) : ListDetailSideEffect
    data class NavigateToEditTask(val householdId: String, val listId: String, val taskId: String) : ListDetailSideEffect
    data object NavigateBack : ListDetailSideEffect
    data class ShowError(val message: UiText) : ListDetailSideEffect
    data class ShowUndo(val operationId: Long, val message: UiText, val isPendingDelete: Boolean) : ListDetailSideEffect
    data class ShowRedo(val operationId: Long, val message: UiText) : ListDetailSideEffect
}
