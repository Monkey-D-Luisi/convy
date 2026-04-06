package com.convy.shared.data.remote

interface PushTokenProvider {
    suspend fun getToken(): String?
}
