package com.convy.app.util

import kotlinx.datetime.Instant
import kotlinx.datetime.LocalDate
import kotlinx.datetime.LocalDateTime
import kotlinx.datetime.TimeZone
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertIs

class TaskDateFormattingTest {
    @Test
    fun `normalizes local picker state to utc`() {
        val result = normalizeTaskDateSelection(
            dueDate = LocalDate(2026, 5, 30),
            reminderLocalDateTime = LocalDateTime(2026, 5, 30, 9, 0),
            timeZone = TimeZone.of("Europe/Madrid"),
            now = Instant.parse("2026-05-29T00:00:00Z"),
        )

        val success = assertIs<TaskDateInputValidation.Success>(result)
        assertEquals("2026-05-30", success.dates.dueDate)
        assertEquals("2026-05-30T07:00:00Z", success.dates.reminderAtUtc)
    }

    @Test
    fun `rejects past reminder selection`() {
        val result = normalizeTaskDateSelection(
            dueDate = null,
            reminderLocalDateTime = LocalDateTime(2026, 5, 30, 9, 0),
            timeZone = TimeZone.UTC,
            now = Instant.parse("2026-05-30T10:00:00Z"),
        )

        assertIs<TaskDateInputValidation.PastReminder>(result)
    }

    @Test
    fun `parses utc reminder into local picker state`() {
        val result = parseTaskReminderLocal(
            "2026-05-30T07:00:00Z",
            TimeZone.of("Europe/Madrid"),
        )

        assertEquals(LocalDateTime(2026, 5, 30, 9, 0), result)
        assertEquals("2026-05-30 09:00", formatTaskDateTime(result))
    }
}
