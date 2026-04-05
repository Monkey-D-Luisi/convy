package com.convy.app.ui.screens.members

import com.convy.shared.domain.repository.HouseholdRepository
import com.convy.shared.domain.repository.InviteRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class MembersStore(
    private val householdId: String,
    private val householdRepository: HouseholdRepository,
    private val inviteRepository: InviteRepository,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val _state = MutableStateFlow(MembersState(householdId = householdId))
    val state: StateFlow<MembersState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<MembersSideEffect>()
    val sideEffects: SharedFlow<MembersSideEffect> = _sideEffects.asSharedFlow()

    init {
        loadMembers()
    }

    fun processIntent(intent: MembersIntent) {
        when (intent) {
            is MembersIntent.Refresh -> loadMembers()
            is MembersIntent.GenerateInvite -> generateInvite()
            is MembersIntent.NavigateBack -> scope.launch {
                _sideEffects.emit(MembersSideEffect.NavigateBack)
            }
        }
    }

    private fun loadMembers() {
        _state.update { it.copy(isLoading = true, error = null) }
        scope.launch {
            householdRepository.getById(householdId).fold(
                onSuccess = { detail ->
                    _state.update { it.copy(members = detail.members, isLoading = false) }
                },
                onFailure = { error ->
                    _state.update { it.copy(isLoading = false, error = error.message ?: "Failed to load members") }
                },
            )
        }
    }

    private fun generateInvite() {
        _state.update { it.copy(isGeneratingInvite = true) }
        scope.launch {
            inviteRepository.create(householdId).fold(
                onSuccess = { invite ->
                    _state.update { it.copy(invite = invite, isGeneratingInvite = false) }
                    _sideEffects.emit(MembersSideEffect.ShareInviteCode(invite.code))
                },
                onFailure = { error ->
                    _state.update { it.copy(isGeneratingInvite = false) }
                    _sideEffects.emit(MembersSideEffect.ShowError(error.message ?: "Failed to generate invite"))
                },
            )
        }
    }
}
