package com.convy.app.ui.screens.households

import com.convy.app.util.UiText
import com.convy.shared.domain.model.Household

data class HouseholdsState(
    val activeHouseholdId: String = "",
    val households: List<Household> = emptyList(),
    val isLoading: Boolean = false,
    val error: UiText? = null,
    val showCreateDialog: Boolean = false,
    val newHouseholdName: String = "",
    val showJoinDialog: Boolean = false,
    val inviteCode: String = "",
    val showRenameDialog: Boolean = false,
    val renameHouseholdId: String = "",
    val renameText: String = "",
    val showLeaveConfirmation: Boolean = false,
    val leaveHouseholdId: String = "",
    val leaveHouseholdName: String = "",
    val isSubmitting: Boolean = false,
)

sealed interface HouseholdsIntent {
    data object Refresh : HouseholdsIntent
    data object NavigateBack : HouseholdsIntent
    data class SelectHousehold(val householdId: String) : HouseholdsIntent
    data object ShowCreateDialog : HouseholdsIntent
    data object DismissCreateDialog : HouseholdsIntent
    data class UpdateNewHouseholdName(val name: String) : HouseholdsIntent
    data object CreateHousehold : HouseholdsIntent
    data object ShowJoinDialog : HouseholdsIntent
    data object DismissJoinDialog : HouseholdsIntent
    data class UpdateInviteCode(val code: String) : HouseholdsIntent
    data object JoinHousehold : HouseholdsIntent
    data class ShowRenameDialog(val householdId: String, val currentName: String) : HouseholdsIntent
    data object DismissRenameDialog : HouseholdsIntent
    data class UpdateRenameText(val text: String) : HouseholdsIntent
    data object ConfirmRename : HouseholdsIntent
    data class ShowLeaveConfirmation(val householdId: String, val householdName: String) : HouseholdsIntent
    data object DismissLeaveConfirmation : HouseholdsIntent
    data object ConfirmLeaveHousehold : HouseholdsIntent
}

sealed interface HouseholdsSideEffect {
    data object NavigateBack : HouseholdsSideEffect
    data class NavigateToLists(val householdId: String) : HouseholdsSideEffect
    data object NavigateToHouseholdSetup : HouseholdsSideEffect
    data class ShowError(val message: String) : HouseholdsSideEffect
}
