package com.convy.app.ui.screens.householdsetup

import com.convy.app.util.UiText

data class HouseholdSetupState(
    val householdName: String = "",
    val inviteCode: String = "",
    val isCreateMode: Boolean = true,
    val isLoading: Boolean = false,
    val error: UiText? = null,
)

sealed interface HouseholdSetupIntent {
    data class UpdateHouseholdName(val name: String) : HouseholdSetupIntent
    data class UpdateInviteCode(val code: String) : HouseholdSetupIntent
    data object ToggleMode : HouseholdSetupIntent
    data object Submit : HouseholdSetupIntent
}

sealed interface HouseholdSetupSideEffect {
    data class NavigateToLists(val householdId: String) : HouseholdSetupSideEffect
}
