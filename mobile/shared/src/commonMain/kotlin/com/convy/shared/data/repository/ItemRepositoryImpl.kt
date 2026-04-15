package com.convy.shared.data.repository

import com.convy.shared.data.offline.OfflineAction
import com.convy.shared.data.offline.OfflineActionQueue
import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.data.remote.dto.BatchCreateItemEntry
import com.convy.shared.data.remote.dto.BatchCreateItemsRequest
import com.convy.shared.data.remote.dto.CreateItemRequest
import com.convy.shared.data.remote.dto.UpdateItemRequest
import com.convy.shared.data.remote.toDomain
import com.convy.shared.domain.model.DuplicateCheck
import com.convy.shared.domain.model.ListItem
import com.convy.shared.domain.model.ParsedItem
import com.convy.shared.domain.model.VoiceParseResult
import com.convy.shared.domain.repository.ItemRepository
import io.ktor.client.plugins.*
import io.ktor.utils.io.errors.*
import kotlin.uuid.ExperimentalUuidApi
import kotlin.uuid.Uuid

class ItemRepositoryImpl(
    private val api: ConvyApi,
    private val offlineQueue: OfflineActionQueue,
) : ItemRepository {

    override suspend fun getByList(listId: String, status: String?, createdBy: String?): Result<List<ListItem>> =
        runCatching {
            api.getListItems(listId, status, createdBy).map { it.toDomain() }
        }

    override suspend fun create(
        listId: String,
        title: String,
        quantity: Int?,
        unit: String?,
        note: String?,
        recurrenceFrequency: Int?,
        recurrenceInterval: Int?,
    ): Result<String> =
        runCatching {
            api.createItem(listId, CreateItemRequest(title, quantity, unit, note, recurrenceFrequency, recurrenceInterval)).id
        }

    override suspend fun update(
        listId: String,
        itemId: String,
        title: String,
        quantity: Int?,
        unit: String?,
        note: String?,
        recurrenceFrequency: Int?,
        recurrenceInterval: Int?,
    ): Result<Unit> =
        runCatching {
            api.updateItem(listId, itemId, UpdateItemRequest(title, quantity, unit, note, recurrenceFrequency, recurrenceInterval))
        }

    @OptIn(ExperimentalUuidApi::class)
    override suspend fun delete(listId: String, itemId: String): Result<Unit> =
        executeOrQueue(
            offlineAction = OfflineAction.DeleteItem(
                id = Uuid.random().toString(),
                listId = listId,
                itemId = itemId,
                createdAt = kotlinx.datetime.Clock.System.now().toEpochMilliseconds(),
            ),
        ) {
            api.deleteItem(listId, itemId)
        }

    @OptIn(ExperimentalUuidApi::class)
    override suspend fun complete(listId: String, itemId: String): Result<Unit> =
        executeOrQueue(
            offlineAction = OfflineAction.CompleteItem(
                id = Uuid.random().toString(),
                listId = listId,
                itemId = itemId,
                createdAt = kotlinx.datetime.Clock.System.now().toEpochMilliseconds(),
            ),
        ) {
            api.completeItem(listId, itemId)
        }

    @OptIn(ExperimentalUuidApi::class)
    override suspend fun uncomplete(listId: String, itemId: String): Result<Unit> =
        executeOrQueue(
            offlineAction = OfflineAction.UncompleteItem(
                id = Uuid.random().toString(),
                listId = listId,
                itemId = itemId,
                createdAt = kotlinx.datetime.Clock.System.now().toEpochMilliseconds(),
            ),
        ) {
            api.uncompleteItem(listId, itemId)
        }

    private suspend fun executeOrQueue(
        offlineAction: OfflineAction,
        apiCall: suspend () -> Unit,
    ): Result<Unit> {
        return try {
            apiCall()
            Result.success(Unit)
        } catch (e: Exception) {
            if (e.isNetworkError()) {
                offlineQueue.enqueue(offlineAction)
                Result.success(Unit)
            } else {
                Result.failure(e)
            }
        }
    }

    private fun Exception.isNetworkError(): Boolean =
        this is HttpRequestTimeoutException ||
            this is IOException ||
            this.cause is IOException

    override suspend fun checkDuplicate(listId: String, title: String): Result<DuplicateCheck> =
        runCatching {
            api.checkDuplicate(listId, title).toDomain()
        }

    override suspend fun getSuggestions(householdId: String, query: String?): Result<List<String>> =
        runCatching {
            api.getItemSuggestions(householdId, query).suggestions
        }

    override suspend fun parseVoiceAudio(listId: String, audioData: ByteArray): Result<VoiceParseResult> =
        try {
            val response = api.parseVoiceAudio(listId, audioData)
            Result.success(
                VoiceParseResult(
                    transcription = response.transcription,
                    items = response.items.map {
                        ParsedItem(it.title, it.quantity, it.unit, it.matchedExistingItem)
                    },
                ),
            )
        } catch (e: HttpRequestTimeoutException) {
            Result.failure(Exception("The voice processing timed out. Please try again with a shorter recording."))
        } catch (e: IOException) {
            Result.failure(Exception("No internet connection. Please check your network and try again."))
        } catch (e: Exception) {
            if (e.cause is IOException) {
                Result.failure(Exception("No internet connection. Please check your network and try again."))
            } else {
                Result.failure(e)
            }
        }

    override suspend fun batchCreate(listId: String, items: List<ParsedItem>): Result<List<String>> =
        try {
            val request = BatchCreateItemsRequest(
                items = items.map { BatchCreateItemEntry(it.title, it.quantity, it.unit) },
            )
            Result.success(api.batchCreateItems(listId, request).createdIds)
        } catch (e: HttpRequestTimeoutException) {
            Result.failure(Exception("The request timed out. Please try again."))
        } catch (e: IOException) {
            Result.failure(Exception("No internet connection. Please check your network and try again."))
        } catch (e: Exception) {
            if (e.cause is IOException) {
                Result.failure(Exception("No internet connection. Please check your network and try again."))
            } else {
                Result.failure(e)
            }
        }
}
