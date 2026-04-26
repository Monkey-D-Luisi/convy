package com.convy.shared.domain.repository

import com.convy.shared.domain.model.User
import com.convy.shared.domain.model.NotificationPreferences

interface UserRepository {
    suspend fun register(firebaseUid: String, displayName: String, email: String): Result<User>
    suspend fun getProfile(): Result<User>
    suspend fun getNotificationPreferences(): Result<NotificationPreferences>
    suspend fun updateNotificationPreferences(preferences: NotificationPreferences): Result<NotificationPreferences>
}
