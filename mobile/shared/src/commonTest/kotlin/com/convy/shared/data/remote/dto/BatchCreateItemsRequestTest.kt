package com.convy.shared.data.remote.dto

import kotlin.test.Test
import kotlin.test.assertContains
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

class BatchCreateItemsRequestTest {

    @Test
    fun `should serialize voice source when batch comes from voice confirmation`() {
        val request = BatchCreateItemsRequest(
            items = listOf(BatchCreateItemEntry(title = "Milk")),
            source = "voice",
        )

        val json = Json.encodeToString(request)

        assertContains(json, "\"source\":\"voice\"")
    }
}
