package com.convy.app.ui.screens.members

import com.convy.shared.domain.model.HouseholdMember
import com.convy.shared.domain.model.Invite
import com.convy.app.util.UiText

data class MembersState(
    val householdId: String = "",
    val members: List<HouseholdMember> = emptyList(),
    val invite: Invite? = null,
    val activeInvites: List<Invite> = emptyList(),
    val isLoading: Boolean = false,
    val isGeneratingInvite: Boolean = false,
    val error: UiText? = null,
)

sealed interface MembersIntent {
    data object Refresh : MembersIntent
    data object GenerateInvite : MembersIntent
    data object CopyInviteCode : MembersIntent
    data class RevokeInvite(val inviteId: String) : MembersIntent
    data object NavigateBack : MembersIntent
}

sealed interface MembersSideEffect {
    data object NavigateBack : MembersSideEffect
    data class ShareInviteCode(val code: String) : MembersSideEffect
    data class ShowError(val message: String) : MembersSideEffect
}
