package com.convy.shared.domain.model

data class ActivityLogEntry(
    val id: String,
    val householdId: String,
    val entityType: String,
    val entityId: String,
    val actionType: String,
    val performedBy: String,
    val performedByName: String,
    val createdAt: String,
    val metadata: String?,
)
