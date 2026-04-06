package com.convy.shared.domain.model

data class ListItem(
    val id: String,
    val title: String,
    val quantity: Int?,
    val unit: String?,
    val note: String?,
    val listId: String,
    val createdBy: String,
    val createdByName: String,
    val createdAt: String,
    val isCompleted: Boolean,
    val completedBy: String?,
    val completedByName: String?,
    val completedAt: String?,
    val recurrenceFrequency: String?,
    val recurrenceInterval: Int?,
    val nextDueDate: String?,
)
