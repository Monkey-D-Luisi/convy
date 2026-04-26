package com.convy.shared.domain.repository

import com.convy.shared.domain.model.TaskItem

interface TaskRepository {
    suspend fun getByList(listId: String, status: String? = null, createdBy: String? = null): Result<List<TaskItem>>
    suspend fun create(listId: String, title: String, note: String?): Result<String>
    suspend fun update(listId: String, taskId: String, title: String, note: String?): Result<Unit>
    suspend fun delete(listId: String, taskId: String): Result<Unit>
    suspend fun complete(listId: String, taskId: String): Result<Unit>
    suspend fun uncomplete(listId: String, taskId: String): Result<Unit>
}
