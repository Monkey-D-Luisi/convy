package com.convy.app.ui.screens.task

import com.convy.shared.domain.model.TaskPriority
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
            dueDate = "2026-05-30",
            reminderAtUtc = "2026-05-30 09:00",
            priority = TaskPriority.High,
        )

        assertEquals("user-2", state.assignedToUserId)
        assertEquals("Marina", state.assignedToUserName)
        assertEquals("2026-05-30", state.dueDate)
        assertEquals("2026-05-30 09:00", state.reminderAtUtc)
        assertEquals(TaskPriority.High, state.priority)
    }
}
