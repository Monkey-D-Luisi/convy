package com.convy.app.ui.screens.settings

import com.convy.shared.domain.model.NotificationPreferences

data class SettingsState(
    val displayName: String = "",
    val email: String = "",
    val householdName: String = "",
    val householdId: String = "",
    val appVersion: String = "",
    val isLoading: Boolean = false,
    val showLeaveConfirmation: Boolean = false,
    val showRenameDialog: Boolean = false,
    val renameText: String = "",
    val isRenaming: Boolean = false,
    val isLeaving: Boolean = false,
    val notificationPreferences: NotificationPreferences = NotificationPreferences(),
    val isSavingNotificationPreferences: Boolean = false,
    val notificationPreferencesError: Boolean = false,
)

enum class NotificationPreferenceKey {
    ItemsAdded,
    TasksAdded,
    ItemsCompleted,
    TasksCompleted,
    ItemTaskChanges,
    ListChanges,
    MemberChanges,
}

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
    data class ToggleNotificationPreference(
        val key: NotificationPreferenceKey,
        val enabled: Boolean,
    ) : SettingsIntent
}

sealed interface SettingsSideEffect {
    data object NavigateToAuth : SettingsSideEffect
    data object NavigateBack : SettingsSideEffect
    data object NavigateToHouseholdSetup : SettingsSideEffect
}
