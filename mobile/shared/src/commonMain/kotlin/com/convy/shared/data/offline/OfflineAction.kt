package com.convy.shared.data.offline

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
sealed interface OfflineAction {
    val id: String
    val createdAt: Long
    val retryCount: Int

    @Serializable
    @SerialName("complete_item")
    data class CompleteItem(
        override val id: String,
        val listId: String,
        val itemId: String,
        override val createdAt: Long,
        override val retryCount: Int = 0,
    ) : OfflineAction

    @Serializable
    @SerialName("uncomplete_item")
    data class UncompleteItem(
        override val id: String,
        val listId: String,
        val itemId: String,
        override val createdAt: Long,
        override val retryCount: Int = 0,
    ) : OfflineAction

    @Serializable
    @SerialName("delete_item")
    data class DeleteItem(
        override val id: String,
        val listId: String,
        val itemId: String,
        override val createdAt: Long,
        override val retryCount: Int = 0,
    ) : OfflineAction

    @Serializable
    @SerialName("complete_task")
    data class CompleteTask(
        override val id: String,
        val listId: String,
        val taskId: String,
        override val createdAt: Long,
        override val retryCount: Int = 0,
    ) : OfflineAction

    @Serializable
    @SerialName("uncomplete_task")
    data class UncompleteTask(
        override val id: String,
        val listId: String,
        val taskId: String,
        override val createdAt: Long,
        override val retryCount: Int = 0,
    ) : OfflineAction

    @Serializable
    @SerialName("delete_task")
    data class DeleteTask(
        override val id: String,
        val listId: String,
        val taskId: String,
        override val createdAt: Long,
        override val retryCount: Int = 0,
    ) : OfflineAction
}

fun OfflineAction.withIncrementedRetry(): OfflineAction = when (this) {
    is OfflineAction.CompleteItem -> copy(retryCount = retryCount + 1)
    is OfflineAction.UncompleteItem -> copy(retryCount = retryCount + 1)
    is OfflineAction.DeleteItem -> copy(retryCount = retryCount + 1)
    is OfflineAction.CompleteTask -> copy(retryCount = retryCount + 1)
    is OfflineAction.UncompleteTask -> copy(retryCount = retryCount + 1)
    is OfflineAction.DeleteTask -> copy(retryCount = retryCount + 1)
}
