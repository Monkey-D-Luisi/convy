package com.convy.app.ui.screens.settings

import com.convy.shared.domain.repository.AuthRepository
import com.convy.shared.domain.repository.HouseholdRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class SettingsStore(
    private val authRepository: AuthRepository,
    private val householdRepository: HouseholdRepository,
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
        }
    }

    private fun loadProfile() {
        scope.launch {
            val user = authRepository.getCurrentUser()
            if (user != null) {
                _state.update {
                    it.copy(
                        displayName = user.displayName,
                        email = user.email,
                    )
                }
            }
            householdRepository.getMyHouseholds().onSuccess { households ->
                if (households.isNotEmpty()) {
                    val household = households.first()
                    _state.update { it.copy(householdName = household.name, householdId = household.id) }
                }
            }
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
            _state.update { it.copy(showLeaveConfirmation = false) }
            _sideEffects.emit(SettingsSideEffect.NavigateToHouseholdSetup)
        }
    }
}
