package com.convy.shared.data.remote

import com.convy.shared.data.remote.dto.*
import io.ktor.client.*
import io.ktor.client.call.*
import io.ktor.client.request.*
import io.ktor.client.request.forms.*
import io.ktor.http.*

class ConvyApi(private val client: HttpClient) {

    // Users
    suspend fun registerUser(request: RegisterUserRequest): UserDto =
        client.post("api/v1/users/register") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun getUserProfile(): UserDto =
        client.get("api/v1/users/me").body()

    // Households
    suspend fun createHousehold(request: CreateHouseholdRequest): IdResponse =
        client.post("api/v1/households") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun getMyHouseholds(): List<HouseholdDto> =
        client.get("api/v1/households").body()

    suspend fun getHousehold(id: String): HouseholdDetailDto =
        client.get("api/v1/households/$id").body()

    suspend fun renameHousehold(id: String, request: RenameHouseholdRequest) {
        client.put("api/v1/households/$id/name") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }
    }

    suspend fun leaveHousehold(id: String) {
        client.post("api/v1/households/$id/leave")
    }

    // Lists
    suspend fun createList(householdId: String, request: CreateListRequest): IdResponse =
        client.post("api/v1/households/$householdId/lists") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun getHouseholdLists(householdId: String, includeArchived: Boolean = false): List<HouseholdListDto> =
        client.get("api/v1/households/$householdId/lists") {
            parameter("includeArchived", includeArchived)
        }.body()

    suspend fun renameList(householdId: String, listId: String, request: RenameListRequest) {
        client.put("api/v1/households/$householdId/lists/$listId/name") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }
    }

    suspend fun archiveList(householdId: String, listId: String) {
        client.post("api/v1/households/$householdId/lists/$listId/archive")
    }

    // Items
    suspend fun createItem(listId: String, request: CreateItemRequest): IdResponse =
        client.post("api/v1/lists/$listId/items") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun getListItems(
        listId: String,
        status: String? = null,
        createdBy: String? = null,
    ): List<ListItemDto> =
        client.get("api/v1/lists/$listId/items") {
            status?.let { parameter("status", it) }
            createdBy?.let { parameter("createdBy", it) }
        }.body()

    suspend fun updateItem(listId: String, itemId: String, request: UpdateItemRequest) {
        client.put("api/v1/lists/$listId/items/$itemId") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }
    }

    suspend fun deleteItem(listId: String, itemId: String) {
        client.delete("api/v1/lists/$listId/items/$itemId")
    }

    suspend fun completeItem(listId: String, itemId: String) {
        client.post("api/v1/lists/$listId/items/$itemId/complete")
    }

    suspend fun uncompleteItem(listId: String, itemId: String) {
        client.post("api/v1/lists/$listId/items/$itemId/uncomplete")
    }

    suspend fun checkDuplicate(listId: String, title: String): DuplicateCheckResponseDto =
        client.get("api/v1/lists/$listId/items/check-duplicate") {
            parameter("title", title)
        }.body()

    suspend fun getItemSuggestions(householdId: String, query: String?): ItemSuggestionsDto =
        client.get("api/v1/households/$householdId/item-suggestions") {
            query?.let { parameter("query", it) }
        }.body()

    // Invites
    suspend fun createInvite(request: CreateInviteRequest): InviteDto =
        client.post("api/v1/invites") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun joinHousehold(request: JoinHouseholdRequest): HouseholdIdResponse =
        client.post("api/v1/invites/join") {
            contentType(ContentType.Application.Json)
            setBody(request)
        }.body()

    suspend fun getHouseholdInvites(householdId: String): List<InviteDto> =
        client.get("api/v1/households/$householdId/invites").body()

    suspend fun revokeInvite(inviteId: String) {
        client.post("api/v1/invites/$inviteId/revoke")
    }

    // Activity
    suspend fun getHouseholdActivity(householdId: String, limit: Int = 50, before: String? = null): List<ActivityLogEntryDto> =
        client.get("api/v1/households/$householdId/activity") {
            parameter("limit", limit)
            before?.let { parameter("before", it) }
        }.body()

    suspend fun getItemHistory(itemId: String): List<ActivityLogEntryDto> =
        client.get("api/v1/items/$itemId/history").body()

    // Voice
    suspend fun parseVoiceAudio(listId: String, audioData: ByteArray): VoiceParseResponseDto =
        client.submitFormWithBinaryData(
            url = "api/v1/lists/$listId/items/parse-voice",
            formData = formData {
                append("audio", audioData, Headers.build {
                    append(HttpHeaders.ContentType, "audio/mp4")
                    append(HttpHeaders.ContentDisposition, "filename=\"recording.m4a\"")
                })
            },
        ).body()

    // Devices
    suspend fun registerDevice(token: String, platform: String) {
        client.post("api/v1/devices/register") {
            contentType(ContentType.Application.Json)
            setBody(RegisterDeviceRequest(token, platform))
        }
    }

    suspend fun unregisterDevice(token: String) {
        client.delete("api/v1/devices/$token")
    }
}
