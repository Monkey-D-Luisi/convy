package com.convy.app.ui.screens.households

import com.convy.app.generated.resources.*
import com.convy.app.ui.mvi.MviStore
import com.convy.app.util.UiText
import com.convy.shared.domain.model.Household
import com.convy.shared.domain.repository.ActiveHouseholdRepository
import com.convy.shared.domain.repository.HouseholdRepository
import com.convy.shared.domain.repository.InviteRepository
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import org.jetbrains.compose.resources.getString

class HouseholdsStore(
    private val initialActiveHouseholdId: String?,
    private val householdRepository: HouseholdRepository,
    private val inviteRepository: InviteRepository,
    private val activeHouseholdRepository: ActiveHouseholdRepository,
) : MviStore() {
    private val _state = MutableStateFlow(HouseholdsState(activeHouseholdId = initialActiveHouseholdId.orEmpty()))
    val state: StateFlow<HouseholdsState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<HouseholdsSideEffect>()
    val sideEffects: SharedFlow<HouseholdsSideEffect> = _sideEffects.asSharedFlow()

    init {
        loadHouseholds()
    }

    fun processIntent(intent: HouseholdsIntent) {
        when (intent) {
            is HouseholdsIntent.Refresh -> loadHouseholds()
            is HouseholdsIntent.NavigateBack -> scope.launch { _sideEffects.emit(HouseholdsSideEffect.NavigateBack) }
            is HouseholdsIntent.SelectHousehold -> selectHousehold(intent.householdId)
            is HouseholdsIntent.ShowCreateDialog -> _state.update {
                it.copy(showCreateDialog = true, newHouseholdName = "", error = null)
            }
            is HouseholdsIntent.DismissCreateDialog -> _state.update {
                it.copy(showCreateDialog = false, newHouseholdName = "")
            }
            is HouseholdsIntent.UpdateNewHouseholdName -> _state.update { it.copy(newHouseholdName = intent.name) }
            is HouseholdsIntent.CreateHousehold -> createHousehold()
            is HouseholdsIntent.ShowJoinDialog -> _state.update {
                it.copy(showJoinDialog = true, inviteCode = "", error = null)
            }
            is HouseholdsIntent.DismissJoinDialog -> _state.update {
                it.copy(showJoinDialog = false, inviteCode = "")
            }
            is HouseholdsIntent.UpdateInviteCode -> _state.update { it.copy(inviteCode = intent.code) }
            is HouseholdsIntent.JoinHousehold -> joinHousehold()
            is HouseholdsIntent.ShowRenameDialog -> _state.update {
                it.copy(
                    showRenameDialog = true,
                    renameHouseholdId = intent.householdId,
                    renameText = intent.currentName,
                )
            }
            is HouseholdsIntent.DismissRenameDialog -> _state.update {
                it.copy(showRenameDialog = false, renameHouseholdId = "", renameText = "")
            }
            is HouseholdsIntent.UpdateRenameText -> _state.update { it.copy(renameText = intent.text) }
            is HouseholdsIntent.ConfirmRename -> renameHousehold()
            is HouseholdsIntent.ShowLeaveConfirmation -> _state.update {
                it.copy(
                    showLeaveConfirmation = true,
                    leaveHouseholdId = intent.householdId,
                    leaveHouseholdName = intent.householdName,
                )
            }
            is HouseholdsIntent.DismissLeaveConfirmation -> _state.update {
                it.copy(showLeaveConfirmation = false, leaveHouseholdId = "", leaveHouseholdName = "")
            }
            is HouseholdsIntent.ConfirmLeaveHousehold -> leaveHousehold()
        }
    }

    private fun loadHouseholds() {
        scope.launch {
            _state.update { it.copy(isLoading = true, error = null) }
            householdRepository.getMyHouseholds().fold(
                onSuccess = { households ->
                    val selected = resolveActiveHousehold(households)
                    _state.update {
                        it.copy(
                            households = households,
                            activeHouseholdId = selected?.id.orEmpty(),
                            isLoading = false,
                        )
                    }
                },
                onFailure = { error ->
                    _state.update {
                        it.copy(
                            isLoading = false,
                            error = UiText.fromError(error.message, Res.string.households_load_failed),
                        )
                    }
                },
            )
        }
    }

    private suspend fun resolveActiveHousehold(households: List<Household>): Household? {
        val initial = initialActiveHouseholdId?.let { activeId -> households.firstOrNull { it.id == activeId } }
        if (initial != null) {
            activeHouseholdRepository.setActiveHouseholdId(initial.id)
            return initial
        }

        return activeHouseholdRepository.resolveActiveHousehold(households)
    }

    private fun selectHousehold(householdId: String) {
        scope.launch {
            activeHouseholdRepository.setActiveHouseholdId(householdId)
            _state.update { it.copy(activeHouseholdId = householdId) }
            _sideEffects.emit(HouseholdsSideEffect.NavigateToLists(householdId))
        }
    }

    private fun createHousehold() {
        val name = _state.value.newHouseholdName.trim()
        if (name.isEmpty() || _state.value.isSubmitting) return

        scope.launch {
            _state.update { it.copy(isSubmitting = true) }
            householdRepository.create(name).fold(
                onSuccess = { householdId ->
                    activeHouseholdRepository.setActiveHouseholdId(householdId)
                    _state.update { it.copy(isSubmitting = false, showCreateDialog = false, newHouseholdName = "") }
                    _sideEffects.emit(HouseholdsSideEffect.NavigateToLists(householdId))
                },
                onFailure = {
                    _state.update { it.copy(isSubmitting = false) }
                    _sideEffects.emit(HouseholdsSideEffect.ShowError(getString(Res.string.households_create_failed)))
                },
            )
        }
    }

    private fun joinHousehold() {
        val inviteCode = _state.value.inviteCode.trim()
        if (inviteCode.isEmpty() || _state.value.isSubmitting) return

        scope.launch {
            _state.update { it.copy(isSubmitting = true) }
            inviteRepository.join(inviteCode).fold(
                onSuccess = { householdId ->
                    activeHouseholdRepository.setActiveHouseholdId(householdId)
                    _state.update { it.copy(isSubmitting = false, showJoinDialog = false, inviteCode = "") }
                    _sideEffects.emit(HouseholdsSideEffect.NavigateToLists(householdId))
                },
                onFailure = {
                    _state.update { it.copy(isSubmitting = false) }
                    _sideEffects.emit(HouseholdsSideEffect.ShowError(getString(Res.string.households_join_failed)))
                },
            )
        }
    }

    private fun renameHousehold() {
        val current = _state.value
        val newName = current.renameText.trim()
        if (newName.isEmpty() || current.isSubmitting) return

        scope.launch {
            _state.update { it.copy(isSubmitting = true) }
            householdRepository.rename(current.renameHouseholdId, newName).fold(
                onSuccess = {
                    _state.update { state ->
                        state.copy(
                            households = state.households.map { household ->
                                if (household.id == current.renameHouseholdId) household.copy(name = newName) else household
                            },
                            showRenameDialog = false,
                            renameHouseholdId = "",
                            renameText = "",
                            isSubmitting = false,
                        )
                    }
                },
                onFailure = {
                    _state.update { it.copy(isSubmitting = false) }
                    _sideEffects.emit(HouseholdsSideEffect.ShowError(getString(Res.string.households_rename_failed)))
                },
            )
        }
    }

    private fun leaveHousehold() {
        val current = _state.value
        val householdId = current.leaveHouseholdId
        if (householdId.isBlank() || current.isSubmitting) return

        scope.launch {
            _state.update { it.copy(isSubmitting = true, showLeaveConfirmation = false) }
            householdRepository.leave(householdId).fold(
                onSuccess = {
                    val remainingHouseholds = householdRepository.getMyHouseholds().getOrNull().orEmpty()
                    val selected = activeHouseholdRepository.resolveActiveHousehold(remainingHouseholds)

                    _state.update {
                        it.copy(
                            households = remainingHouseholds,
                            activeHouseholdId = selected?.id.orEmpty(),
                            leaveHouseholdId = "",
                            leaveHouseholdName = "",
                            isSubmitting = false,
                        )
                    }

                    if (selected == null) {
                        _sideEffects.emit(HouseholdsSideEffect.NavigateToHouseholdSetup)
                    } else if (householdId == current.activeHouseholdId) {
                        _sideEffects.emit(HouseholdsSideEffect.NavigateToLists(selected.id))
                    }
                },
                onFailure = {
                    _state.update { it.copy(isSubmitting = false, leaveHouseholdId = "", leaveHouseholdName = "") }
                    _sideEffects.emit(HouseholdsSideEffect.ShowError(getString(Res.string.households_leave_failed)))
                },
            )
        }
    }
}
