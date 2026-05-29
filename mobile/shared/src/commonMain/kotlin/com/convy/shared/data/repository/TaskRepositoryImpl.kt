package com.convy.shared.data.repository

import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.data.remote.dto.BatchCreateTaskEntry
import com.convy.shared.data.remote.dto.BatchCreateTasksRequest
import com.convy.shared.data.remote.dto.CreateTaskRequest
import com.convy.shared.data.remote.dto.UpdateTaskRequest
import com.convy.shared.data.remote.toDomain
import com.convy.shared.domain.model.ParsedTask
import com.convy.shared.domain.model.TaskItem
import com.convy.shared.domain.model.TaskPriority
import com.convy.shared.domain.model.TaskVoiceParseResult
import com.convy.shared.domain.repository.TaskRepository
import io.ktor.client.plugins.HttpRequestTimeoutException
import io.ktor.utils.io.errors.IOException
import kotlinx.datetime.Clock
import kotlinx.datetime.TimeZone
import kotlin.coroutines.cancellation.CancellationException

class TaskRepositoryImpl(
    private val api: ConvyApi,
) : TaskRepository {

    override suspend fun getByList(listId: String, status: String?, createdBy: String?): Result<List<TaskItem>> =
        cancellableRunCatching {
            api.getListTasks(listId, status, createdBy).map { it.toDomain() }
        }

    override suspend fun create(
        listId: String,
        title: String,
        note: String?,
        assignedToUserId: String?,
        dueDate: String?,
        reminderAtUtc: String?,
        priority: TaskPriority,
    ): Result<String> =
        cancellableRunCatching {
            api.createTask(
                listId,
                CreateTaskRequest(
                    title = title,
                    note = note,
                    assignedToUserId = assignedToUserId,
                    dueDate = dueDate,
                    reminderAtUtc = reminderAtUtc,
                    priority = priority.name,
                ),
            ).id
        }

    override suspend fun update(
        listId: String,
        taskId: String,
        title: String,
        note: String?,
        assignedToUserId: String?,
        dueDate: String?,
        reminderAtUtc: String?,
        priority: TaskPriority,
    ): Result<Unit> =
        cancellableRunCatching {
            api.updateTask(
                listId,
                taskId,
                UpdateTaskRequest(
                    title = title,
                    note = note,
                    assignedToUserId = assignedToUserId,
                    dueDate = dueDate,
                    reminderAtUtc = reminderAtUtc,
                    priority = priority.name,
                ),
            )
        }

    override suspend fun delete(listId: String, taskId: String): Result<Unit> =
        cancellableRunCatching {
            api.deleteTask(listId, taskId)
        }

    override suspend fun complete(listId: String, taskId: String): Result<Unit> =
        cancellableRunCatching {
            api.completeTask(listId, taskId)
        }

    override suspend fun uncomplete(listId: String, taskId: String): Result<Unit> =
        cancellableRunCatching {
            api.uncompleteTask(listId, taskId)
        }

    override suspend fun parseVoiceAudio(listId: String, audioData: ByteArray): Result<TaskVoiceParseResult> =
        try {
            val response = api.parseTaskVoiceAudio(
                listId = listId,
                audioData = audioData,
                timeZoneId = TimeZone.currentSystemDefault().id,
                now = Clock.System.now().toString(),
            )
            Result.success(
                TaskVoiceParseResult(
                    transcription = response.transcription,
                    tasks = response.tasks.map { it.toDomain() },
                ),
            )
        } catch (e: HttpRequestTimeoutException) {
            Result.failure(Exception("The voice processing timed out. Please try again with a shorter recording."))
        } catch (e: IOException) {
            Result.failure(Exception("No internet connection. Please check your network and try again."))
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            if (e.cause is IOException) {
                Result.failure(Exception("No internet connection. Please check your network and try again."))
            } else {
                Result.failure(e)
            }
        }

    override suspend fun batchCreate(listId: String, tasks: List<ParsedTask>): Result<Unit> =
        try {
            api.batchCreateTasks(
                listId,
                BatchCreateTasksRequest(
                    tasks = tasks.map {
                        BatchCreateTaskEntry(
                            title = it.title,
                            note = it.note,
                            assignedToUserId = it.assignedToUserId,
                            dueDate = it.dueDate,
                            reminderAtUtc = it.reminderAtUtc,
                            priority = it.priority.name,
                        )
                    },
                ),
            )
            Result.success(Unit)
        } catch (e: HttpRequestTimeoutException) {
            Result.failure(Exception("The request timed out. Please try again."))
        } catch (e: IOException) {
            Result.failure(Exception("No internet connection. Please check your network and try again."))
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            if (e.cause is IOException) {
                Result.failure(Exception("No internet connection. Please check your network and try again."))
            } else {
                Result.failure(e)
            }
        }
}
