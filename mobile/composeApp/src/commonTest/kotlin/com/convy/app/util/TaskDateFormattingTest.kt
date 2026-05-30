package com.convy.app.util

import kotlinx.datetime.Instant
import kotlinx.datetime.TimeZone
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertIs

class TaskDateFormattingTest {
    @Test
    fun `normalizes local reminder input to utc`() {
        val result = normalizeTaskDateInputs(
            dueDateInput = "2026-05-30",
            reminderInput = "2026-05-30 09:00",
            timeZone = TimeZone.of("Europe/Madrid"),
            now = Instant.parse("2026-05-29T00:00:00Z"),
        )

        val success = assertIs<TaskDateInputValidation.Success>(result)
        assertEquals("2026-05-30", success.dates.dueDate)
        assertEquals("2026-05-30T07:00:00Z", success.dates.reminderAtUtc)
    }

    @Test
    fun `rejects invalid due date input`() {
        val result = normalizeTaskDateInputs(
            dueDateInput = "30-05-2026",
            reminderInput = "",
            timeZone = TimeZone.UTC,
            now = Instant.parse("2026-05-29T00:00:00Z"),
        )

        assertIs<TaskDateInputValidation.InvalidDueDate>(result)
    }

    @Test
    fun `rejects past reminder input`() {
        val result = normalizeTaskDateInputs(
            dueDateInput = "",
            reminderInput = "2026-05-30 09:00",
            timeZone = TimeZone.UTC,
            now = Instant.parse("2026-05-30T10:00:00Z"),
        )

        assertIs<TaskDateInputValidation.PastReminder>(result)
    }

    @Test
    fun `formats utc reminder in local time`() {
        val result = formatTaskReminderLocal(
            "2026-05-30T07:00:00Z",
            TimeZone.of("Europe/Madrid"),
        )

        assertEquals("2026-05-30 09:00", result)
    }
}
