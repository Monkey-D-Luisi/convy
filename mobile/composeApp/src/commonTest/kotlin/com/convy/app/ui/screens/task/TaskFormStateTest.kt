package com.convy.app.ui.screens.task

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
}
