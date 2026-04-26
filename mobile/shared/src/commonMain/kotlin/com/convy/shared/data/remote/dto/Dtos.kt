package com.convy.shared.data.remote.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class UserDto(
    @SerialName("id") val id: String,
    @SerialName("displayName") val displayName: String,
    @SerialName("email") val email: String,
    @SerialName("createdAt") val createdAt: String,
)

@Serializable
data class NotificationPreferencesDto(
    @SerialName("itemsAdded") val itemsAdded: Boolean,
    @SerialName("tasksAdded") val tasksAdded: Boolean,
    @SerialName("itemsCompleted") val itemsCompleted: Boolean,
    @SerialName("tasksCompleted") val tasksCompleted: Boolean,
    @SerialName("itemTaskChanges") val itemTaskChanges: Boolean,
    @SerialName("listChanges") val listChanges: Boolean,
    @SerialName("memberChanges") val memberChanges: Boolean,
)

@Serializable
data class HouseholdDto(
    @SerialName("id") val id: String,
    @SerialName("name") val name: String,
    @SerialName("createdBy") val createdBy: String,
    @SerialName("createdAt") val createdAt: String,
)

@Serializable
data class HouseholdDetailDto(
    @SerialName("id") val id: String,
    @SerialName("name") val name: String,
    @SerialName("createdBy") val createdBy: String,
    @SerialName("createdAt") val createdAt: String,
    @SerialName("members") val members: List<HouseholdMemberDto>,
)

@Serializable
data class HouseholdMemberDto(
    @SerialName("userId") val userId: String,
    @SerialName("displayName") val displayName: String,
    @SerialName("email") val email: String,
    @SerialName("role") val role: String,
    @SerialName("joinedAt") val joinedAt: String,
)

@Serializable
data class HouseholdListDto(
    @SerialName("id") val id: String,
    @SerialName("name") val name: String,
    @SerialName("type") val type: String,
    @SerialName("householdId") val householdId: String,
    @SerialName("createdBy") val createdBy: String,
    @SerialName("createdAt") val createdAt: String,
    @SerialName("isArchived") val isArchived: Boolean,
    @SerialName("archivedAt") val archivedAt: String?,
)

@Serializable
data class ListItemDto(
    @SerialName("id") val id: String,
    @SerialName("title") val title: String,
    @SerialName("quantity") val quantity: Int?,
    @SerialName("unit") val unit: String?,
    @SerialName("note") val note: String?,
    @SerialName("listId") val listId: String,
    @SerialName("createdBy") val createdBy: String,
    @SerialName("createdByName") val createdByName: String,
    @SerialName("createdAt") val createdAt: String,
    @SerialName("isCompleted") val isCompleted: Boolean,
    @SerialName("completedBy") val completedBy: String?,
    @SerialName("completedByName") val completedByName: String?,
    @SerialName("completedAt") val completedAt: String?,
    @SerialName("recurrenceFrequency") val recurrenceFrequency: String? = null,
    @SerialName("recurrenceInterval") val recurrenceInterval: Int? = null,
    @SerialName("nextDueDate") val nextDueDate: String? = null,
)

@Serializable
data class TaskItemDto(
    @SerialName("id") val id: String,
    @SerialName("title") val title: String,
    @SerialName("note") val note: String?,
    @SerialName("listId") val listId: String,
    @SerialName("createdBy") val createdBy: String,
    @SerialName("createdByName") val createdByName: String,
    @SerialName("createdAt") val createdAt: String,
    @SerialName("isCompleted") val isCompleted: Boolean,
    @SerialName("completedBy") val completedBy: String?,
    @SerialName("completedByName") val completedByName: String?,
    @SerialName("completedAt") val completedAt: String?,
)

@Serializable
data class InviteDto(
    @SerialName("id") val id: String,
    @SerialName("householdId") val householdId: String,
    @SerialName("code") val code: String,
    @SerialName("expiresAt") val expiresAt: String,
    @SerialName("isValid") val isValid: Boolean,
    @SerialName("createdAt") val createdAt: String,
)

@Serializable
data class DuplicateCheckResponseDto(
    @SerialName("hasPotentialDuplicates") val hasPotentialDuplicates: Boolean,
    @SerialName("potentialDuplicates") val potentialDuplicates: List<DuplicateItemDto>,
)

@Serializable
data class DuplicateItemDto(
    @SerialName("id") val id: String,
    @SerialName("title") val title: String,
    @SerialName("quantity") val quantity: Int?,
    @SerialName("unit") val unit: String?,
)

@Serializable
data class ItemSuggestionsDto(
    @SerialName("suggestions") val suggestions: List<String>,
)

@Serializable
data class ParsedItemDto(
    @SerialName("title") val title: String,
    @SerialName("quantity") val quantity: Int?,
    @SerialName("unit") val unit: String?,
    @SerialName("matchedExistingItem") val matchedExistingItem: String? = null,
)

@Serializable
data class VoiceParseResponseDto(
    @SerialName("transcription") val transcription: String,
    @SerialName("items") val items: List<ParsedItemDto>,
)

@Serializable
data class IdResponse(
    @SerialName("id") val id: String,
)

@Serializable
data class BatchCreateResponse(
    @SerialName("createdIds") val createdIds: List<String>,
)

@Serializable
data class HouseholdIdResponse(
    @SerialName("householdId") val householdId: String,
)

@Serializable
data class ErrorResponse(
    @SerialName("code") val code: String,
    @SerialName("message") val message: String,
)

@Serializable
data class ActivityLogEntryDto(
    @SerialName("id") val id: String,
    @SerialName("householdId") val householdId: String,
    @SerialName("entityType") val entityType: String,
    @SerialName("entityId") val entityId: String,
    @SerialName("actionType") val actionType: String,
    @SerialName("performedBy") val performedBy: String,
    @SerialName("performedByName") val performedByName: String,
    @SerialName("createdAt") val createdAt: String,
    @SerialName("metadata") val metadata: String?,
)
