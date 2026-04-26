package com.convy.shared.data.repository

import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.data.remote.dto.CreateTaskRequest
import com.convy.shared.data.remote.dto.UpdateTaskRequest
import com.convy.shared.data.remote.toDomain
import com.convy.shared.domain.model.TaskItem
import com.convy.shared.domain.repository.TaskRepository

class TaskRepositoryImpl(
    private val api: ConvyApi,
) : TaskRepository {

    override suspend fun getByList(listId: String, status: String?, createdBy: String?): Result<List<TaskItem>> =
        runCatching {
            api.getListTasks(listId, status, createdBy).map { it.toDomain() }
        }

    override suspend fun create(listId: String, title: String, note: String?): Result<String> =
        runCatching {
            api.createTask(listId, CreateTaskRequest(title, note)).id
        }

    override suspend fun update(listId: String, taskId: String, title: String, note: String?): Result<Unit> =
        runCatching {
            api.updateTask(listId, taskId, UpdateTaskRequest(title, note))
        }

    override suspend fun delete(listId: String, taskId: String): Result<Unit> =
        runCatching {
            api.deleteTask(listId, taskId)
        }

    override suspend fun complete(listId: String, taskId: String): Result<Unit> =
        runCatching {
            api.completeTask(listId, taskId)
        }

    override suspend fun uncomplete(listId: String, taskId: String): Result<Unit> =
        runCatching {
            api.uncompleteTask(listId, taskId)
        }
}
