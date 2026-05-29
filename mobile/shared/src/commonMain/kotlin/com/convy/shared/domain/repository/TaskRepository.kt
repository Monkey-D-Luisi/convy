package com.convy.shared.domain.repository

import com.convy.shared.domain.model.ParsedTask
import com.convy.shared.domain.model.TaskItem
import com.convy.shared.domain.model.TaskPriority
import com.convy.shared.domain.model.TaskVoiceParseResult

interface TaskRepository {
    suspend fun getByList(listId: String, status: String? = null, createdBy: String? = null): Result<List<TaskItem>>
    suspend fun create(
        listId: String,
        title: String,
        note: String?,
        assignedToUserId: String? = null,
        dueDate: String? = null,
        reminderAtUtc: String? = null,
        priority: TaskPriority = TaskPriority.Normal,
    ): Result<String>
    suspend fun update(
        listId: String,
        taskId: String,
        title: String,
        note: String?,
        assignedToUserId: String? = null,
        dueDate: String? = null,
        reminderAtUtc: String? = null,
        priority: TaskPriority = TaskPriority.Normal,
    ): Result<Unit>
    suspend fun delete(listId: String, taskId: String): Result<Unit>
    suspend fun complete(listId: String, taskId: String): Result<Unit>
    suspend fun uncomplete(listId: String, taskId: String): Result<Unit>
    suspend fun parseVoiceAudio(listId: String, audioData: ByteArray): Result<TaskVoiceParseResult>
    suspend fun batchCreate(listId: String, tasks: List<ParsedTask>): Result<Unit>
}
