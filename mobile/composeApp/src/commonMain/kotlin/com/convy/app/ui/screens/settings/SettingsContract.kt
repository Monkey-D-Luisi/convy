package com.convy.app.ui.screens.settings

data class SettingsState(
    val displayName: String = "",
    val email: String = "",
    val householdName: String = "",
    val householdId: String = "",
    val isLoading: Boolean = false,
    val showLeaveConfirmation: Boolean = false,
    val showRenameDialog: Boolean = false,
    val renameText: String = "",
    val isRenaming: Boolean = false,
    val isLeaving: Boolean = false,
)

sealed interface SettingsIntent {
    data object SignOut : SettingsIntent
    data object NavigateBack : SettingsIntent
    data object ShowLeaveConfirmation : SettingsIntent
    data object DismissLeaveConfirmation : SettingsIntent
    data object ConfirmLeaveHousehold : SettingsIntent
    data object ShowRenameDialog : SettingsIntent
    data object DismissRenameDialog : SettingsIntent
    data class UpdateRenameText(val text: String) : SettingsIntent
    data object ConfirmRename : SettingsIntent
}

sealed interface SettingsSideEffect {
    data object NavigateToAuth : SettingsSideEffect
    data object NavigateBack : SettingsSideEffect
    data object NavigateToHouseholdSetup : SettingsSideEffect
}
