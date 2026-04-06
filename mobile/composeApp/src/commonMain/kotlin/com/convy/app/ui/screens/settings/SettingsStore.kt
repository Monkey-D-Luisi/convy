package com.convy.app.ui.screens.settings

import com.convy.shared.domain.repository.AuthRepository
import com.convy.shared.domain.repository.HouseholdRepository
import com.convy.shared.domain.repository.UserRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class SettingsStore(
    private val authRepository: AuthRepository,
    private val householdRepository: HouseholdRepository,
    private val userRepository: UserRepository,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val _state = MutableStateFlow(SettingsState())
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
            householdRepository.getMyHouseholds().onSuccess { households ->
                if (households.isNotEmpty()) {
                    val household = households.first()
                    _state.update { it.copy(householdName = household.name, householdId = household.id) }
                }
            }
            _state.update { it.copy(isLoading = false) }
        }
    }

    private fun signOut() {
        scope.launch {
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
                    _sideEffects.emit(SettingsSideEffect.NavigateToHouseholdSetup)
                },
                onFailure = {
                    _state.update { it.copy(isLeaving = false) }
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
                },
            )
        }
    }
}
