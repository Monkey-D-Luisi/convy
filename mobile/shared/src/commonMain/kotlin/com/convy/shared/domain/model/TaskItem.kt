package com.convy.shared.domain.model

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
)
