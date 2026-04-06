package com.convy.shared.data.remote.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class RegisterUserRequest(
    @SerialName("firebaseUid") val firebaseUid: String,
    @SerialName("displayName") val displayName: String,
    @SerialName("email") val email: String,
)

@Serializable
data class CreateHouseholdRequest(
    @SerialName("name") val name: String,
)

@Serializable
data class RenameHouseholdRequest(
    @SerialName("newName") val newName: String,
)

@Serializable
data class CreateListRequest(
    @SerialName("name") val name: String,
    @SerialName("type") val type: String,
)

@Serializable
data class RenameListRequest(
    @SerialName("newName") val newName: String,
)

@Serializable
data class CreateItemRequest(
    @SerialName("title") val title: String,
    @SerialName("quantity") val quantity: Int? = null,
    @SerialName("unit") val unit: String? = null,
    @SerialName("note") val note: String? = null,
    @SerialName("recurrenceFrequency") val recurrenceFrequency: Int? = null,
    @SerialName("recurrenceInterval") val recurrenceInterval: Int? = null,
)

@Serializable
data class UpdateItemRequest(
    @SerialName("title") val title: String,
    @SerialName("quantity") val quantity: Int? = null,
    @SerialName("unit") val unit: String? = null,
    @SerialName("note") val note: String? = null,
    @SerialName("recurrenceFrequency") val recurrenceFrequency: Int? = null,
    @SerialName("recurrenceInterval") val recurrenceInterval: Int? = null,
)

@Serializable
data class CreateInviteRequest(
    @SerialName("householdId") val householdId: String,
)

@Serializable
data class JoinHouseholdRequest(
    @SerialName("inviteCode") val inviteCode: String,
)

@Serializable
data class RegisterDeviceRequest(
    @SerialName("token") val token: String,
    @SerialName("platform") val platform: String,
)
