package com.convy.shared.domain.repository

import com.convy.shared.domain.model.DuplicateCheck
import com.convy.shared.domain.model.ListItem
import com.convy.shared.domain.model.VoiceParseResult

interface ItemRepository {
    suspend fun getByList(listId: String, status: String? = null, createdBy: String? = null): Result<List<ListItem>>
    suspend fun create(listId: String, title: String, quantity: Int?, unit: String?, note: String?, recurrenceFrequency: Int? = null, recurrenceInterval: Int? = null): Result<String>
    suspend fun update(listId: String, itemId: String, title: String, quantity: Int?, unit: String?, note: String?, recurrenceFrequency: Int? = null, recurrenceInterval: Int? = null): Result<Unit>
    suspend fun delete(listId: String, itemId: String): Result<Unit>
    suspend fun complete(listId: String, itemId: String): Result<Unit>
    suspend fun uncomplete(listId: String, itemId: String): Result<Unit>
    suspend fun checkDuplicate(listId: String, title: String): Result<DuplicateCheck>
    suspend fun getSuggestions(householdId: String, query: String?): Result<List<String>>
    suspend fun parseVoiceAudio(listId: String, audioData: ByteArray): Result<VoiceParseResult>
}
