package com.convy.shared.data.repository

import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.data.remote.dto.RegisterUserRequest
import com.convy.shared.data.remote.toDomain
import com.convy.shared.domain.model.User
import com.convy.shared.domain.repository.UserRepository

class UserRepositoryImpl(
    private val api: ConvyApi,
) : UserRepository {

    override suspend fun register(firebaseUid: String, displayName: String, email: String): Result<User> =
        runCatching {
            val response = api.registerUser(
                RegisterUserRequest(firebaseUid, displayName, email)
            )
            response.toDomain()
        }

    override suspend fun getProfile(): Result<User> =
        runCatching {
            api.getUserProfile().toDomain()
        }
}
