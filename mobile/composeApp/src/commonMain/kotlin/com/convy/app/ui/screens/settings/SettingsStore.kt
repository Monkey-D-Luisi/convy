package com.convy.app.ui.screens.settings

import com.convy.app.platform.AppInfoProvider
import com.convy.app.ui.mvi.MviStore
import com.convy.shared.domain.repository.AuthRepository
import com.convy.shared.domain.repository.ActiveHouseholdRepository
import com.convy.shared.domain.repository.HouseholdRepository
import com.convy.shared.domain.repository.UserRepository
import com.convy.shared.domain.model.NotificationPreferences
import com.convy.app.generated.resources.*
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import org.jetbrains.compose.resources.getString

class SettingsStore(
    private val householdId: String,
    private val authRepository: AuthRepository,
    private val householdRepository: HouseholdRepository,
    private val userRepository: UserRepository,
    appInfoProvider: AppInfoProvider,
    private val activeHouseholdRepository: ActiveHouseholdRepository,
) : MviStore() {
    private val _state = MutableStateFlow(SettingsState(appVersion = appInfoProvider.versionName))
    val state: StateFlow<SettingsState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<SettingsSideEffect>()
    val sideEffects: SharedFlow<SettingsSideEffect> = _sideEffects.asSharedFlow()

    init {
        loadProfile()
    }

    fun processIntent(intent: SettingsIntent) {
        when (intent) {
            is SettingsIntent.SignOut -> signOut()
            is SettingsIntent.NavigateBack -> scope.launch {
                _sideEffects.emit(SettingsSideEffect.NavigateBack)
            }
            is SettingsIntent.ShowLeaveConfirmation -> _state.update { it.copy(showLeaveConfirmation = true) }
            is SettingsIntent.DismissLeaveConfirmation -> _state.update { it.copy(showLeaveConfirmation = false) }
            is SettingsIntent.ConfirmLeaveHousehold -> leaveHousehold()
            is SettingsIntent.ManageHouseholds -> scope.launch {
                _sideEffects.emit(SettingsSideEffect.NavigateToHouseholds(_state.value.householdId))
            }
            is SettingsIntent.ShowRenameDialog -> _state.update {
                it.copy(showRenameDialog = true, renameText = it.householdName)
            }
            is SettingsIntent.DismissRenameDialog -> _state.update {
                it.copy(showRenameDialog = false, renameText = "")
            }
            is SettingsIntent.UpdateRenameText -> _state.update {
                it.copy(renameText = intent.text)
            }
            is SettingsIntent.ConfirmRename -> renameHousehold()
            is SettingsIntent.ToggleNotificationPreference -> updateNotificationPreference(intent.key, intent.enabled)
        }
    }

    private fun loadProfile() {
        scope.launch {
            _state.update { it.copy(isLoading = true) }
            val user = authRepository.getCurrentUser()
            if (user != null) {
                _state.update {
                    it.copy(
                        displayName = user.displayName,
                        email = user.email,
                    )
                }
            }
            // Also fetch from backend for accuracy
            userRepository.getProfile().onSuccess { profile ->
                _state.update {
                    it.copy(
                        displayName = profile.displayName,
                        email = profile.email,
                    )
                }
            }
            userRepository.getNotificationPreferences().onSuccess { preferences ->
                _state.update { it.copy(notificationPreferences = preferences, notificationPreferencesError = false) }
            }
            householdRepository.getMyHouseholds().onSuccess { households ->
                val household = households.firstOrNull { it.id == householdId }
                    ?: activeHouseholdRepository.resolveActiveHousehold(households)
                if (household != null) {
                    activeHouseholdRepository.setActiveHouseholdId(household.id)
                    _state.update { it.copy(householdName = household.name, householdId = household.id) }
                }
            }
            _state.update { it.copy(isLoading = false) }
        }
    }

    private fun signOut() {
        scope.launch {
            activeHouseholdRepository.clearActiveHouseholdId()
            authRepository.signOut()
            _sideEffects.emit(SettingsSideEffect.NavigateToAuth)
        }
    }

    private fun leaveHousehold() {
        scope.launch {
            _state.update { it.copy(showLeaveConfirmation = false, isLeaving = true) }
            val householdId = _state.value.householdId
            householdRepository.leave(householdId).fold(
                onSuccess = {
                    _state.update { it.copy(isLeaving = false) }
                    val remainingHouseholds = householdRepository.getMyHouseholds().getOrNull().orEmpty()
                    val nextHousehold = activeHouseholdRepository.resolveActiveHousehold(remainingHouseholds)
                    if (nextHousehold == null) {
                        _sideEffects.emit(SettingsSideEffect.NavigateToHouseholdSetup)
                    } else {
                        _sideEffects.emit(SettingsSideEffect.NavigateToLists(nextHousehold.id))
                    }
                },
                onFailure = {
                    _state.update { it.copy(isLeaving = false) }
                    _sideEffects.emit(SettingsSideEffect.ShowError(getString(Res.string.settings_leave_failed)))
                },
            )
        }
    }

    private fun renameHousehold() {
        val newName = _state.value.renameText.trim()
        if (newName.isEmpty()) return
        scope.launch {
            _state.update { it.copy(isRenaming = true) }
            val householdId = _state.value.householdId
            householdRepository.rename(householdId, newName).fold(
                onSuccess = {
                    _state.update {
                        it.copy(
                            householdName = newName,
                            showRenameDialog = false,
                            renameText = "",
                            isRenaming = false,
                        )
                    }
                },
                onFailure = {
                    _state.update { it.copy(isRenaming = false) }
                    _sideEffects.emit(SettingsSideEffect.ShowError(getString(Res.string.settings_rename_failed)))
                },
            )
        }
    }

    private fun updateNotificationPreference(key: NotificationPreferenceKey, enabled: Boolean) {
        val updated = _state.value.notificationPreferences.withValue(key, enabled)
        _state.update {
            it.copy(
                notificationPreferences = updated,
                isSavingNotificationPreferences = true,
                notificationPreferencesError = false,
            )
        }

        scope.launch {
            userRepository.updateNotificationPreferences(updated).fold(
                onSuccess = { preferences ->
                    _state.update {
                        it.copy(
                            notificationPreferences = preferences,
                            isSavingNotificationPreferences = false,
                            notificationPreferencesError = false,
                        )
                    }
                },
                onFailure = {
                    _state.update {
                        it.copy(
                            isSavingNotificationPreferences = false,
                            notificationPreferencesError = true,
                        )
                    }
                },
            )
        }
    }

    private fun NotificationPreferences.withValue(
        key: NotificationPreferenceKey,
        enabled: Boolean,
    ): NotificationPreferences = when (key) {
        NotificationPreferenceKey.ItemsAdded -> copy(itemsAdded = enabled)
        NotificationPreferenceKey.TasksAdded -> copy(tasksAdded = enabled)
        NotificationPreferenceKey.ItemsCompleted -> copy(itemsCompleted = enabled)
        NotificationPreferenceKey.TasksCompleted -> copy(tasksCompleted = enabled)
        NotificationPreferenceKey.ItemTaskChanges -> copy(itemTaskChanges = enabled)
        NotificationPreferenceKey.ListChanges -> copy(listChanges = enabled)
        NotificationPreferenceKey.MemberChanges -> copy(memberChanges = enabled)
    }
}
