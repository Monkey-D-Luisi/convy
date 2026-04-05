package com.convy.app.ui.screens.settings

data class SettingsState(
    val displayName: String = "",
    val email: String = "",
    val householdName: String = "",
    val isLoading: Boolean = false,
)

sealed interface SettingsIntent {
    data object SignOut : SettingsIntent
    data object NavigateBack : SettingsIntent
}

sealed interface SettingsSideEffect {
    data object NavigateToAuth : SettingsSideEffect
    data object NavigateBack : SettingsSideEffect
}
