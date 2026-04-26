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
        parseEvent(message)
    }

    suspend fun connect(householdId: String) {
        signalRClient.connect(householdId)
    }

    suspend fun disconnect() {
        signalRClient.disconnect()
    }

    private fun parseEvent(message: SignalRMessage): HouseholdEvent? {
        return try {
            when (message.target) {
                "ItemCreated" -> {
                    val item = json.decodeFromJsonElement<ListItemDto>(message.arguments[0])
                    HouseholdEvent.ItemCreated(item)
                }
                "ItemUpdated" -> {
                    val item = json.decodeFromJsonElement<ListItemDto>(message.arguments[0])
                    HouseholdEvent.ItemUpdated(item)
                }
                "ItemCompleted" -> {
                    val item = json.decodeFromJsonElement<ListItemDto>(message.arguments[0])
                    HouseholdEvent.ItemCompleted(item)
                }
                "ItemUncompleted" -> {
                    val item = json.decodeFromJsonElement<ListItemDto>(message.arguments[0])
                    HouseholdEvent.ItemUncompleted(item)
                }
                "ItemDeleted" -> {
                    val itemId = message.arguments[0].jsonPrimitive.content
                    HouseholdEvent.ItemDeleted(itemId)
                }
                "TaskCreated" -> {
                    val task = json.decodeFromJsonElement<TaskItemDto>(message.arguments[0])
                    HouseholdEvent.TaskCreated(task)
                }
                "TaskUpdated" -> {
                    val task = json.decodeFromJsonElement<TaskItemDto>(message.arguments[0])
                    HouseholdEvent.TaskUpdated(task)
                }
                "TaskCompleted" -> {
                    val task = json.decodeFromJsonElement<TaskItemDto>(message.arguments[0])
                    HouseholdEvent.TaskCompleted(task)
                }
                "TaskUncompleted" -> {
                    val task = json.decodeFromJsonElement<TaskItemDto>(message.arguments[0])
                    HouseholdEvent.TaskUncompleted(task)
                }
                "TaskDeleted" -> {
                    val taskId = message.arguments[0].jsonPrimitive.content
                    HouseholdEvent.TaskDeleted(taskId)
                }
                "ListCreated" -> {
                    val obj = message.arguments[0].jsonObject
                    HouseholdEvent.ListCreated(
                        listId = obj["listId"]!!.jsonPrimitive.content,
                        listName = obj["listName"]!!.jsonPrimitive.content
                    )
                }
                "ListRenamed" -> {
                    val obj = message.arguments[0].jsonObject
                    HouseholdEvent.ListRenamed(
                        listId = obj["listId"]!!.jsonPrimitive.content,
                        newName = obj["newName"]!!.jsonPrimitive.content
                    )
                }
                "ListArchived" -> {
                    val obj = message.arguments[0].jsonObject
                    HouseholdEvent.ListArchived(
                        listId = obj["listId"]!!.jsonPrimitive.content
                    )
                }
                "MemberJoined" -> {
                    val obj = message.arguments[0].jsonObject
                    HouseholdEvent.MemberJoined(
                        userId = obj["userId"]!!.jsonPrimitive.content,
                        displayName = obj["displayName"]!!.jsonPrimitive.content
                    )
                }
                "HouseholdRenamed" -> {
                    val obj = message.arguments[0].jsonObject
                    HouseholdEvent.HouseholdRenamed(
                        householdId = obj["householdId"]!!.jsonPrimitive.content,
                        newName = obj["newName"]!!.jsonPrimitive.content
                    )
                }
                "MemberLeft" -> {
                    val obj = message.arguments[0].jsonObject
                    HouseholdEvent.MemberLeft(
                        userId = obj["userId"]!!.jsonPrimitive.content,
                        displayName = obj["displayName"]!!.jsonPrimitive.content
                    )
                }
                else -> null
            }
        } catch (_: Exception) {
            null
        }
    }
}
