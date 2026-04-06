package com.convy.shared.data.remote

class DeviceTokenManager(
    private val api: ConvyApi,
    private val pushTokenProvider: PushTokenProvider,
) {
    suspend fun registerCurrentToken() {
        try {
            val token = pushTokenProvider.getToken() ?: return
            api.registerDevice(token, "Android")
        } catch (_: Exception) {
            // Silent failure — will retry on next app start
        }
    }
}
