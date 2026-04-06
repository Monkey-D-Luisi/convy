package com.convy.shared.domain.repository

import com.convy.shared.domain.model.User

interface AuthRepository {
    suspend fun signIn(email: String, password: String): Result<User>
    suspend fun signUp(email: String, password: String, displayName: String): Result<User>
    suspend fun signInWithGoogle(): Result<User>
    suspend fun signOut()
    suspend fun getCurrentUser(): User?
    suspend fun getIdToken(): String?
}
