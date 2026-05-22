package com.convy.shared.data.remote

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put

class HouseholdRealtimeServiceTest {
    private val json = Json {
        ignoreUnknownKeys = true
        isLenient = true
    }

    @Test
    fun parseHouseholdEventReturnsNullWhenArgumentsAreMissing() {
        val event = parseHouseholdEvent(json, SignalRMessage("ListRenamed", JsonArray(emptyList())))

        assertNull(event)
    }

    @Test
    fun parseHouseholdEventReturnsNullWhenRequiredFieldsAreMissing() {
        val message = SignalRMessage(
            target = "ListRenamed",
            arguments = JsonArray(
                listOf(
                    buildJsonObject {
                        put("listId", "list-1")
                    },
                ),
            ),
        )

        val event = parseHouseholdEvent(json, message)

        assertNull(event)
    }

    @Test
    fun parseHouseholdEventParsesListRenamedPayload() {
        val message = SignalRMessage(
            target = "ListRenamed",
            arguments = JsonArray(
                listOf(
                    buildJsonObject {
                        put("listId", "list-1")
                        put("newName", "Groceries")
                    },
                ),
            ),
        )

        val event = parseHouseholdEvent(json, message)

        assertEquals(HouseholdEvent.ListRenamed("list-1", "Groceries"), event)
    }
}
