package com.convy.app.util

import kotlinx.datetime.Clock
import kotlinx.datetime.Instant
import kotlinx.datetime.LocalDate
import kotlinx.datetime.LocalDateTime
import kotlinx.datetime.TimeZone
import kotlinx.datetime.toInstant
import kotlinx.datetime.toLocalDateTime

data class NormalizedTaskDateInputs(
    val dueDate: String?,
    val reminderAtUtc: String?,
)

sealed interface TaskDateInputValidation {
    data class Success(val dates: NormalizedTaskDateInputs) : TaskDateInputValidation
    data object InvalidDueDate : TaskDateInputValidation
    data object InvalidReminder : TaskDateInputValidation
    data object PastReminder : TaskDateInputValidation
}

fun normalizeTaskDateInputs(
    dueDateInput: String,
    reminderInput: String,
    timeZone: TimeZone = TimeZone.currentSystemDefault(),
    now: Instant = Clock.System.now(),
): TaskDateInputValidation {
    val dueDate = when (val dueDate = parseDueDateInput(dueDateInput)) {
        DateInputParseResult.Invalid -> return TaskDateInputValidation.InvalidDueDate
        is DateInputParseResult.Success -> dueDate.value
    }
    val reminder = when (val reminder = parseReminderInput(reminderInput, timeZone)) {
        DateInputParseResult.Invalid -> return TaskDateInputValidation.InvalidReminder
        is DateInputParseResult.Success -> reminder.value
    }

    if (reminder != null && reminder <= now) {
        return TaskDateInputValidation.PastReminder
    }

    return TaskDateInputValidation.Success(
        NormalizedTaskDateInputs(
            dueDate = dueDate,
            reminderAtUtc = reminder?.toString(),
        ),
    )
}

fun formatTaskReminderLocal(
    reminderAtUtc: String?,
    timeZone: TimeZone = TimeZone.currentSystemDefault(),
): String? =
    reminderAtUtc
        ?.takeIf { it.isNotBlank() }
        ?.let { value ->
            runCatching {
                Instant.parse(value).toLocalDateTime(timeZone).toDisplayDateTime()
            }.getOrNull()
        }

fun formatInstantLocal(
    instantUtc: String?,
    timeZone: TimeZone = TimeZone.currentSystemDefault(),
): String? =
    instantUtc
        ?.takeIf { it.isNotBlank() }
        ?.let { value ->
            runCatching {
                Instant.parse(value).toLocalDateTime(timeZone).toDisplayDateTime()
            }.getOrNull()
        }

private sealed interface DateInputParseResult<out T> {
    data class Success<T>(val value: T?) : DateInputParseResult<T>
    data object Invalid : DateInputParseResult<Nothing>
}

private fun parseDueDateInput(value: String): DateInputParseResult<String> {
    val trimmed = value.trim()
    if (trimmed.isEmpty()) {
        return DateInputParseResult.Success(null)
    }

    return runCatching { DateInputParseResult.Success(LocalDate.parse(trimmed).toString()) }
        .getOrElse { DateInputParseResult.Invalid }
}

private fun parseReminderInput(value: String, timeZone: TimeZone): DateInputParseResult<Instant> {
    val trimmed = value.trim()
    if (trimmed.isEmpty()) {
        return DateInputParseResult.Success(null)
    }

    val normalized = trimmed.replace(" ", "T")
    val normalizedWithSeconds = if (normalized.length == LOCAL_DATE_TIME_MINUTE_LENGTH) {
        "$normalized:00"
    } else {
        normalized
    }

    val local = runCatching { LocalDateTime.parse(normalizedWithSeconds) }.getOrNull()
    return local?.let { DateInputParseResult.Success(it.toInstant(timeZone)) }
        ?: DateInputParseResult.Invalid
}

private fun LocalDateTime.toDisplayDateTime(): String =
    "${date} ${hour.twoDigits()}:${minute.twoDigits()}"

private fun Int.twoDigits(): String = toString().padStart(2, '0')

private const val LOCAL_DATE_TIME_MINUTE_LENGTH = 16
