package com.convy.shared.platform

interface FileStorage {
    suspend fun read(filename: String): String?
    suspend fun write(filename: String, content: String)
}
