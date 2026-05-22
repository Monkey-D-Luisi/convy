package com.convy.shared.data.remote

import com.convy.shared.data.remote.dto.ListItemDto
import com.convy.shared.data.remote.dto.TaskItemDto
import kotlinx.coroutines.flow.*
import kotlinx.serialization.json.*

sealed interface HouseholdEvent {
    data class ItemCreated(val item: ListItemDto) : HouseholdEvent
    data class ItemUpdated(val item: ListItemDto) : HouseholdEvent
    data class ItemCompleted(val item: ListItemDto) : HouseholdEvent
    data class ItemUncompleted(val item: ListItemDto) : HouseholdEvent
    data class ItemDeleted(val itemId: String) : HouseholdEvent
    data class TaskCreated(val task: TaskItemDto) : HouseholdEvent
    data class TaskUpdated(val task: TaskItemDto) : HouseholdEvent
    data class TaskCompleted(val task: TaskItemDto) : HouseholdEvent
    data class TaskUncompleted(val task: TaskItemDto) : HouseholdEvent
    data class TaskDeleted(val taskId: String) : HouseholdEvent
    data class ListCreated(val listId: String, val listName: String) : HouseholdEvent
    data class ListRenamed(val listId: String, val newName: String) : HouseholdEvent
    data class ListArchived(val listId: String) : HouseholdEvent
    data class MemberJoined(val userId: String, val displayName: String) : HouseholdEvent
    data class HouseholdRenamed(val householdId: String, val newName: String) : HouseholdEvent
    data class MemberLeft(val userId: String, val displayName: String) : HouseholdEvent
}

class HouseholdRealtimeService(
    private val signalRClient: SignalRClient,
    private val json: Json
) {
    val events: Flow<HouseholdEvent> = signalRClient.messages.mapNotNull { message ->
        parseHouseholdEvent(json, message)
    }

    suspend fun connect(householdId: String) {
        signalRClient.connect(householdId)
    }

    suspend fun disconnect() {
        signalRClient.disconnect()
    }
}

internal fun parseHouseholdEvent(json: Json, message: SignalRMessage): HouseholdEvent? {
    val payload = message.arguments.getOrNull(0) ?: return null

    return try {
        when (message.target) {
            "ItemCreated" -> HouseholdEvent.ItemCreated(json.decodeFromJsonElement<ListItemDto>(payload))
            "ItemUpdated" -> HouseholdEvent.ItemUpdated(json.decodeFromJsonElement<ListItemDto>(payload))
            "ItemCompleted" -> HouseholdEvent.ItemCompleted(json.decodeFromJsonElement<ListItemDto>(payload))
            "ItemUncompleted" -> HouseholdEvent.ItemUncompleted(json.decodeFromJsonElement<ListItemDto>(payload))
            "ItemDeleted" -> payload.jsonPrimitive.contentOrNull?.let(HouseholdEvent::ItemDeleted)
            "TaskCreated" -> HouseholdEvent.TaskCreated(json.decodeFromJsonElement<TaskItemDto>(payload))
            "TaskUpdated" -> HouseholdEvent.TaskUpdated(json.decodeFromJsonElement<TaskItemDto>(payload))
            "TaskCompleted" -> HouseholdEvent.TaskCompleted(json.decodeFromJsonElement<TaskItemDto>(payload))
            "TaskUncompleted" -> HouseholdEvent.TaskUncompleted(json.decodeFromJsonElement<TaskItemDto>(payload))
            "TaskDeleted" -> payload.jsonPrimitive.contentOrNull?.let(HouseholdEvent::TaskDeleted)
            "ListCreated" -> payload.jsonObject.eventString("listId")?.let { listId ->
                payload.jsonObject.eventString("listName")?.let { listName ->
                    HouseholdEvent.ListCreated(listId, listName)
                }
            }
            "ListRenamed" -> payload.jsonObject.eventString("listId")?.let { listId ->
                payload.jsonObject.eventString("newName")?.let { newName ->
                    HouseholdEvent.ListRenamed(listId, newName)
                }
            }
            "ListArchived" -> payload.jsonObject.eventString("listId")?.let(HouseholdEvent::ListArchived)
            "MemberJoined" -> payload.jsonObject.eventString("userId")?.let { userId ->
                payload.jsonObject.eventString("displayName")?.let { displayName ->
                    HouseholdEvent.MemberJoined(userId, displayName)
                }
            }
            "HouseholdRenamed" -> payload.jsonObject.eventString("householdId")?.let { householdId ->
                payload.jsonObject.eventString("newName")?.let { newName ->
                    HouseholdEvent.HouseholdRenamed(householdId, newName)
                }
            }
            "MemberLeft" -> payload.jsonObject.eventString("userId")?.let { userId ->
                payload.jsonObject.eventString("displayName")?.let { displayName ->
                    HouseholdEvent.MemberLeft(userId, displayName)
                }
            }
            else -> null
        }
    } catch (_: Exception) {
        null
    }
}

private fun JsonObject.eventString(name: String): String? =
    this[name]?.jsonPrimitive?.contentOrNull?.takeIf { it.isNotBlank() }
