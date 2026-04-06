package com.convy.app.ui.screens.members

import com.convy.shared.domain.model.HouseholdMember
import com.convy.shared.domain.model.Invite

data class MembersState(
    val householdId: String = "",
    val members: List<HouseholdMember> = emptyList(),
    val invite: Invite? = null,
    val isLoading: Boolean = false,
    val isGeneratingInvite: Boolean = false,
    val error: String? = null,
)

sealed interface MembersIntent {
    data object Refresh : MembersIntent
    data object GenerateInvite : MembersIntent
    data object CopyInviteCode : MembersIntent
    data object NavigateBack : MembersIntent
}

sealed interface MembersSideEffect {
    data object NavigateBack : MembersSideEffect
    data class ShareInviteCode(val code: String) : MembersSideEffect
    data class ShowError(val message: String) : MembersSideEffect
}
