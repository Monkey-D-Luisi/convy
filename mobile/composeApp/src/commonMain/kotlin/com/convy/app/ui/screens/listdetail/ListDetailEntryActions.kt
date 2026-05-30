package com.convy.app.ui.screens.listdetail

import com.convy.shared.domain.repository.ItemRepository
import com.convy.shared.domain.repository.TaskRepository

class ListDetailEntryActions(
    private val itemRepository: ItemRepository,
    private val taskRepository: TaskRepository,
) {
    suspend fun load(
        isTaskList: Boolean,
        listId: String,
        status: String?,
        createdBy: String?,
    ): Result<List<ListEntryUi>> =
        if (isTaskList) {
            taskRepository.getByList(listId, status, createdBy).map { tasks ->
                tasks.map { it.toListEntryUi() }
            }
        } else {
            itemRepository.getByList(listId, status, createdBy).map { items ->
                items.map { it.toListEntryUi() }
            }
        }

    suspend fun setCompletion(
        isTaskList: Boolean,
        listId: String,
        entryId: String,
        completed: Boolean,
    ): Result<Unit> =
        if (isTaskList) {
            if (completed) taskRepository.complete(listId, entryId) else taskRepository.uncomplete(listId, entryId)
        } else {
            if (completed) itemRepository.complete(listId, entryId) else itemRepository.uncomplete(listId, entryId)
        }

    suspend fun delete(isTaskList: Boolean, listId: String, entryId: String): Result<Unit> =
        if (isTaskList) taskRepository.delete(listId, entryId) else itemRepository.delete(listId, entryId)
}
