package com.convy.app.ui.screens.lists

import com.convy.shared.domain.model.HouseholdList
import com.convy.shared.domain.model.ListType

data class HouseholdListsState(
    val householdId: String = "",
    val householdName: String = "",
    val lists: List<HouseholdList> = emptyList(),
    val pendingCounts: Map<String, Int> = emptyMap(),
    val isLoading: Boolean = false,
    val error: String? = null,
    val showCreateDialog: Boolean = false,
    val newListName: String = "",
    val newListType: ListType = ListType.Shopping,
    val showRenameDialog: Boolean = false,
    val renameListId: String = "",
    val renameListName: String = "",
    val showArchiveConfirmation: Boolean = false,
    val archiveListId: String = "",
    val archiveListName: String = "",
)

sealed interface HouseholdListsIntent {
    data object Refresh : HouseholdListsIntent
    data class OpenList(val listId: String, val listName: String, val listType: String) : HouseholdListsIntent
    data object ShowCreateDialog : HouseholdListsIntent
    data object DismissCreateDialog : HouseholdListsIntent
    data class UpdateNewListName(val name: String) : HouseholdListsIntent
    data class UpdateNewListType(val type: ListType) : HouseholdListsIntent
    data object CreateList : HouseholdListsIntent
    data object OpenMembers : HouseholdListsIntent
    data object OpenActivity : HouseholdListsIntent
    data object OpenSettings : HouseholdListsIntent
    data class ShowRenameDialog(val listId: String, val currentName: String) : HouseholdListsIntent
    data object DismissRenameDialog : HouseholdListsIntent
    data class UpdateRenameListName(val name: String) : HouseholdListsIntent
    data object ConfirmRenameList : HouseholdListsIntent
    data class ShowArchiveConfirmation(val listId: String, val listName: String) : HouseholdListsIntent
    data object DismissArchiveConfirmation : HouseholdListsIntent
    data object ConfirmArchiveList : HouseholdListsIntent
}

sealed interface HouseholdListsSideEffect {
    data class NavigateToList(val householdId: String, val listId: String, val listName: String, val listType: String) : HouseholdListsSideEffect
    data class NavigateToMembers(val householdId: String) : HouseholdListsSideEffect
    data class NavigateToActivity(val householdId: String) : HouseholdListsSideEffect
    data object NavigateToSettings : HouseholdListsSideEffect
    data class ShowError(val message: String) : HouseholdListsSideEffect
}
