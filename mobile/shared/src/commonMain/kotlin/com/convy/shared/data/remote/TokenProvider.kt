package com.convy.shared.data.remote

interface TokenProvider {
    suspend fun getToken(): String?
}
