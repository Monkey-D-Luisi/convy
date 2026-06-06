package com.convy.app.ui.screens.task

import com.convy.shared.domain.model.TaskPriority
import kotlinx.datetime.LocalDate
import kotlinx.datetime.LocalDateTime
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse

class TaskFormStateTest {
    @Test
    fun `task form contains task fields only`() {
        val state = TaskFormState(title = "Clean kitchen", note = "Before dinner")

        assertEquals("Clean kitchen", state.title)
        assertEquals("Before dinner", state.note)
        assertFalse(state.isEditing)
    }

    @Test
    fun `task form stores structured coordination fields`() {
        val state = TaskFormState(
            title = "Clean kitchen",
            assignedToUserId = "user-2",
            assignedToUserName = "Marina",
            dueDate = LocalDate(2026, 5, 30),
            reminderLocalDateTime = LocalDateTime(2026, 5, 30, 9, 0),
            priority = TaskPriority.High,
        )

        assertEquals("user-2", state.assignedToUserId)
        assertEquals("Marina", state.assignedToUserName)
        assertEquals(LocalDate(2026, 5, 30), state.dueDate)
        assertEquals(LocalDateTime(2026, 5, 30, 9, 0), state.reminderLocalDateTime)
        assertEquals(TaskPriority.High, state.priority)
    }

    @Test
    fun `task title longer than supported limit cannot be saved`() {
        val state = TaskFormState(title = "a".repeat(81))

        assertFalse(state.canSave)
        assertEquals(81, state.titleLength)
        assertEquals(80, TaskTitleMaxLength)
    }
}
