package com.convy.app.ui.screens.lists

import com.convy.shared.domain.model.HouseholdList
import com.convy.shared.domain.model.ListType

data class HouseholdListsState(
    val householdId: String = "",
    val householdName: String = "",
    val lists: List<HouseholdList> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null,
    val showCreateDialog: Boolean = false,
    val newListName: String = "",
    val newListType: ListType = ListType.Shopping,
)

sealed interface HouseholdListsIntent {
    data object Refresh : HouseholdListsIntent
    data class OpenList(val listId: String, val listName: String) : HouseholdListsIntent
    data object ShowCreateDialog : HouseholdListsIntent
    data object DismissCreateDialog : HouseholdListsIntent
    data class UpdateNewListName(val name: String) : HouseholdListsIntent
    data class UpdateNewListType(val type: ListType) : HouseholdListsIntent
    data object CreateList : HouseholdListsIntent
    data object OpenMembers : HouseholdListsIntent
    data object OpenSettings : HouseholdListsIntent
}

sealed interface HouseholdListsSideEffect {
    data class NavigateToList(val householdId: String, val listId: String, val listName: String) : HouseholdListsSideEffect
    data class NavigateToMembers(val householdId: String) : HouseholdListsSideEffect
    data object NavigateToSettings : HouseholdListsSideEffect
    data class ShowError(val message: String) : HouseholdListsSideEffect
}
