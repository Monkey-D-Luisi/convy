package com.convy.shared.platform

interface GoogleSignInHelper {
    suspend fun getGoogleIdToken(): String
}
