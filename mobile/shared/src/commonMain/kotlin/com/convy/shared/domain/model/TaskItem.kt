package com.convy.shared.domain.model

enum class TaskPriority {
    Low,
    Normal,
    High,
}

data class TaskItem(
    val id: String,
    val title: String,
    val note: String?,
    val listId: String,
    val createdBy: String,
    val createdByName: String,
    val createdAt: String,
    val isCompleted: Boolean,
    val completedBy: String?,
    val completedByName: String?,
    val completedAt: String?,
    val assignedToUserId: String? = null,
    val assignedToUserName: String? = null,
    val dueDate: String? = null,
    val reminderAtUtc: String? = null,
    val reminderSentAtUtc: String? = null,
    val priority: TaskPriority = TaskPriority.Normal,
)
