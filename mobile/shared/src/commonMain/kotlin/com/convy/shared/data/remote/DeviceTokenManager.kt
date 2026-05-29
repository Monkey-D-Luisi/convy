package com.convy.shared.data.remote

import com.convy.shared.platform.LocaleProvider
import kotlin.coroutines.cancellation.CancellationException

class DeviceTokenManager(
    private val api: ConvyApi,
    private val pushTokenProvider: PushTokenProvider,
    private val localeProvider: LocaleProvider,
) {
    suspend fun registerCurrentToken() {
        try {
            val token = pushTokenProvider.getToken() ?: return
            api.registerDevice(token, "Android", localeProvider.getLanguageTag())
        } catch (e: CancellationException) {
            throw e
        } catch (_: Exception) {
            // Silent failure; will retry on next app start.
        }
    }
}
