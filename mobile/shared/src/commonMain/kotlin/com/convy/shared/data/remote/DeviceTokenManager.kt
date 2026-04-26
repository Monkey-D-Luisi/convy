package com.convy.shared.data.remote

import com.convy.shared.platform.LocaleProvider

class DeviceTokenManager(
    private val api: ConvyApi,
    private val pushTokenProvider: PushTokenProvider,
    private val localeProvider: LocaleProvider,
) {
    suspend fun registerCurrentToken() {
        try {
            val token = pushTokenProvider.getToken() ?: return
            api.registerDevice(token, "Android", localeProvider.getLanguageTag())
        } catch (_: Exception) {
            // Silent failure; will retry on next app start.
        }
    }
}
