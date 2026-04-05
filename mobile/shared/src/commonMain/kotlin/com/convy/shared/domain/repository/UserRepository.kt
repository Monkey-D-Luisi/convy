package com.convy.shared.domain.repository

import com.convy.shared.domain.model.User

interface UserRepository {
    suspend fun register(firebaseUid: String, displayName: String, email: String): Result<User>
}
