package com.convy.shared.data.remote.dto

import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import kotlin.test.Test
import kotlin.test.assertTrue

class RegisterDeviceRequestTest {
    @Test
    fun `serializes optional locale when registering device`() {
        val payload = Json.encodeToString(RegisterDeviceRequest("token-1", "Android", "es-ES"))

        assertTrue(payload.contains("\"locale\":\"es-ES\""))
    }
}
