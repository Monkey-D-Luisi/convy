package com.convy.shared.domain.model

data class NotificationPreferences(
    val itemsAdded: Boolean = true,
    val tasksAdded: Boolean = true,
    val itemsCompleted: Boolean = false,
    val tasksCompleted: Boolean = false,
    val itemTaskChanges: Boolean = false,
    val listChanges: Boolean = true,
    val memberChanges: Boolean = true,
)
