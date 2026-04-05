package com.convy.shared.data.repository

import com.convy.shared.data.remote.TokenProvider
import com.convy.shared.domain.model.User
import com.convy.shared.domain.repository.AuthRepository

class StubAuthRepository : AuthRepository, TokenProvider {

    private var currentUser: User? = null
    private var token: String? = null

    override suspend fun signIn(email: String, password: String): Result<User> {
        val user = User(
            id = "stub-user-id",
            displayName = email.substringBefore("@"),
            email = email,
            createdAt = "",
        )
        currentUser = user
        token = "stub-firebase-token"
        return Result.success(user)
    }

    override suspend fun signUp(email: String, password: String, displayName: String): Result<User> {
        val user = User(
            id = "stub-user-id",
            displayName = displayName,
            email = email,
            createdAt = "",
        )
        currentUser = user
        token = "stub-firebase-token"
        return Result.success(user)
    }

    override suspend fun signOut() {
        currentUser = null
        token = null
    }

    override suspend fun getCurrentUser(): User? = currentUser

    override suspend fun getIdToken(): String? = token

    override suspend fun getToken(): String? = token
}
