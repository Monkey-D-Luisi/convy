package com.convy.shared.domain.repository

import com.convy.shared.domain.model.Invite

interface InviteRepository {
    suspend fun create(householdId: String): Result<Invite>
    suspend fun join(inviteCode: String): Result<String>
}
