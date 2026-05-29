package com.convy.shared.data.remote

import com.convy.shared.data.remote.dto.TaskItemDto
import com.convy.shared.data.remote.dto.ParsedTaskDto
import com.convy.shared.domain.model.TaskPriority
import kotlin.test.Test
import kotlin.test.assertEquals

class TaskDtoMappingTest {
    @Test
    fun `task dto maps structured metadata`() {
        val task = TaskItemDto(
            id = "task-1",
            title = "Clean kitchen",
            note = "Before dinner",
            listId = "list-1",
            createdBy = "user-1",
            createdByName = "Luis",
            assignedToUserId = "user-2",
            assignedToUserName = "Marina",
            dueDate = "2026-05-30",
            reminderAtUtc = "2026-05-30T07:00:00Z",
            reminderSentAtUtc = null,
            priority = "High",
            createdAt = "2026-05-29T10:00:00Z",
            isCompleted = false,
            completedBy = null,
            completedByName = null,
            completedAt = null,
        ).toDomain()

        assertEquals("user-2", task.assignedToUserId)
        assertEquals("Marina", task.assignedToUserName)
        assertEquals("2026-05-30", task.dueDate)
        assertEquals("2026-05-30T07:00:00Z", task.reminderAtUtc)
        assertEquals(TaskPriority.High, task.priority)
    }

    @Test
    fun `parsed task dto maps voice metadata`() {
        val task = ParsedTaskDto(
            title = "Clean kitchen",
            note = "Before dinner",
            assignedToUserId = "user-2",
            dueDate = "2026-05-30",
            reminderAtUtc = "2026-05-30T07:00:00Z",
            priority = "High",
            matchedExistingTask = "task-1",
        ).toDomain()

        assertEquals("Clean kitchen", task.title)
        assertEquals("user-2", task.assignedToUserId)
        assertEquals("2026-05-30", task.dueDate)
        assertEquals(TaskPriority.High, task.priority)
        assertEquals("task-1", task.matchedExistingTask)
    }
}
