package com.convy.shared.data.repository

import com.convy.shared.data.remote.TokenProvider
import com.convy.shared.domain.model.User
import com.convy.shared.domain.repository.AuthRepository
import dev.gitlive.firebase.Firebase
import dev.gitlive.firebase.auth.GoogleAuthProvider
import dev.gitlive.firebase.auth.auth

class FirebaseAuthRepository : AuthRepository, TokenProvider {

    private val auth = Firebase.auth

    override suspend fun signIn(email: String, password: String): Result<User> {
        return try {
            val result = auth.signInWithEmailAndPassword(email, password)
            val firebaseUser = result.user ?: return Result.failure(Exception("Sign in failed"))
            Result.success(firebaseUser.toUser())
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    override suspend fun signUp(email: String, password: String, displayName: String): Result<User> {
        return try {
            val result = auth.createUserWithEmailAndPassword(email, password)
            val firebaseUser = result.user ?: return Result.failure(Exception("Sign up failed"))
            firebaseUser.updateProfile(displayName = displayName)
            Result.success(firebaseUser.toUser(displayName))
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    override suspend fun signInWithGoogle(): Result<User> {
        return Result.failure(Exception("Google Sign-In is not available yet. Please use email and password."))
    }

    override suspend fun signOut() {
        auth.signOut()
    }

    override suspend fun getCurrentUser(): User? {
        return auth.currentUser?.toUser()
    }

    override suspend fun getIdToken(): String? {
        return auth.currentUser?.getIdToken(false)
    }

    override suspend fun getToken(): String? {
        return getIdToken()
    }

    private fun dev.gitlive.firebase.auth.FirebaseUser.toUser(name: String? = null): User {
        return User(
            id = uid,
            displayName = name ?: displayName ?: email?.substringBefore("@") ?: "User",
            email = email ?: "",
            createdAt = "",
        )
    }
}
