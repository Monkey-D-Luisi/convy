package com.convy.app.ui.screens.householdsetup

import com.convy.shared.domain.repository.HouseholdRepository
import com.convy.shared.domain.repository.InviteRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class HouseholdSetupStore(
    private val householdRepository: HouseholdRepository,
    private val inviteRepository: InviteRepository,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val _state = MutableStateFlow(HouseholdSetupState())
    val state: StateFlow<HouseholdSetupState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<HouseholdSetupSideEffect>()
    val sideEffects: SharedFlow<HouseholdSetupSideEffect> = _sideEffects.asSharedFlow()

    fun processIntent(intent: HouseholdSetupIntent) {
        when (intent) {
            is HouseholdSetupIntent.UpdateHouseholdName -> _state.update { it.copy(householdName = intent.name) }
            is HouseholdSetupIntent.UpdateInviteCode -> _state.update { it.copy(inviteCode = intent.code) }
            is HouseholdSetupIntent.ToggleMode -> _state.update { it.copy(isCreateMode = !it.isCreateMode, error = null) }
            is HouseholdSetupIntent.Submit -> submit()
        }
    }

    private fun submit() {
        val current = _state.value
        if (current.isLoading) return

        _state.update { it.copy(isLoading = true, error = null) }

        scope.launch {
            if (current.isCreateMode) {
                householdRepository.create(current.householdName).fold(
                    onSuccess = { householdId ->
                        _state.update { it.copy(isLoading = false) }
                        _sideEffects.emit(HouseholdSetupSideEffect.NavigateToLists(householdId))
                    },
                    onFailure = { error ->
                        _state.update { it.copy(isLoading = false, error = error.message ?: "Failed to create household") }
                    },
                )
            } else {
                inviteRepository.join(current.inviteCode).fold(
                    onSuccess = { householdId ->
                        _state.update { it.copy(isLoading = false) }
                        _sideEffects.emit(HouseholdSetupSideEffect.NavigateToLists(householdId))
                    },
                    onFailure = { error ->
                        _state.update { it.copy(isLoading = false, error = error.message ?: "Failed to join household") }
                    },
                )
            }
        }
    }
}
