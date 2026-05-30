package com.convy.shared.domain.model

data class ParsedTask(
    val title: String,
    val note: String? = null,
    val assignedToUserId: String? = null,
    val assignedToUserName: String? = null,
    val dueDate: String? = null,
    val reminderAtUtc: String? = null,
    val priority: TaskPriority = TaskPriority.Normal,
    val matchedExistingTask: String? = null,
    val isSelected: Boolean = true,
)

data class TaskVoiceParseResult(
    val transcription: String,
    val tasks: List<ParsedTask>,
)
