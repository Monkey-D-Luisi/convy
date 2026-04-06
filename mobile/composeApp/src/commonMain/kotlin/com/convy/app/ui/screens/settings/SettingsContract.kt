package com.convy.app.ui.screens.settings

data class SettingsState(
    val displayName: String = "",
    val email: String = "",
    val householdName: String = "",
    val householdId: String = "",
    val isLoading: Boolean = false,
    val showLeaveConfirmation: Boolean = false,
)

sealed interface SettingsIntent {
    data object SignOut : SettingsIntent
    data object NavigateBack : SettingsIntent
    data object ShowLeaveConfirmation : SettingsIntent
    data object DismissLeaveConfirmation : SettingsIntent
    data object ConfirmLeaveHousehold : SettingsIntent
}

sealed interface SettingsSideEffect {
    data object NavigateToAuth : SettingsSideEffect
    data object NavigateBack : SettingsSideEffect
    data object NavigateToHouseholdSetup : SettingsSideEffect
}
