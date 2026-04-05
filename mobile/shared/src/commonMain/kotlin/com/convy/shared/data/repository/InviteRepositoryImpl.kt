package com.convy.shared.data.repository

import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.data.remote.dto.CreateInviteRequest
import com.convy.shared.data.remote.dto.JoinHouseholdRequest
import com.convy.shared.data.remote.toDomain
import com.convy.shared.domain.model.Invite
import com.convy.shared.domain.repository.InviteRepository

class InviteRepositoryImpl(
    private val api: ConvyApi,
) : InviteRepository {

    override suspend fun create(householdId: String): Result<Invite> =
        runCatching {
            api.createInvite(CreateInviteRequest(householdId)).toDomain()
        }

    override suspend fun join(inviteCode: String): Result<String> =
        runCatching {
            api.joinHousehold(JoinHouseholdRequest(inviteCode)).householdId
        }
}
