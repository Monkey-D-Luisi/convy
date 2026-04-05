package com.convy.shared.domain.model

data class HouseholdList(
    val id: String,
    val name: String,
    val type: ListType,
    val householdId: String,
    val createdBy: String,
    val createdAt: String,
    val isArchived: Boolean,
    val archivedAt: String?,
)

enum class ListType {
    Shopping,
    Tasks,
}
