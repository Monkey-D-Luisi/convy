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
        loadInvites()
    }

    fun processIntent(intent: MembersIntent) {
        when (intent) {
            is MembersIntent.Refresh -> {
                loadMembers()
                loadInvites()
            }
            is MembersIntent.GenerateInvite -> generateInvite()
            is MembersIntent.CopyInviteCode -> scope.launch {
                val invite = _state.value.invite
                if (invite != null) {
                    _sideEffects.emit(MembersSideEffect.ShareInviteCode(invite.code))
                }
            }
            is MembersIntent.RevokeInvite -> revokeInvite(intent.inviteId)
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

    private fun loadInvites() {
        scope.launch {
            inviteRepository.getByHousehold(householdId).onSuccess { invites ->
                _state.update { it.copy(activeInvites = invites) }
            }
        }
    }

    private fun generateInvite() {
        _state.update { it.copy(isGeneratingInvite = true) }
        scope.launch {
            inviteRepository.create(householdId).fold(
                onSuccess = { invite ->
                    _state.update { it.copy(invite = invite, isGeneratingInvite = false) }
                    _sideEffects.emit(MembersSideEffect.ShareInviteCode(invite.code))
                    loadInvites()
                },
                onFailure = { error ->
                    _state.update { it.copy(isGeneratingInvite = false) }
                    _sideEffects.emit(MembersSideEffect.ShowError(error.message ?: "Failed to generate invite"))
                },
            )
        }
    }

    private fun revokeInvite(inviteId: String) {
        scope.launch {
            inviteRepository.revoke(inviteId).fold(
                onSuccess = { loadInvites() },
                onFailure = { error ->
                    _sideEffects.emit(MembersSideEffect.ShowError(error.message ?: "Failed to revoke invite"))
                },
            )
        }
    }
}
