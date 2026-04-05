package com.convy.shared.data.repository

import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.data.remote.dto.CreateItemRequest
import com.convy.shared.data.remote.dto.UpdateItemRequest
import com.convy.shared.data.remote.toDomain
import com.convy.shared.domain.model.DuplicateCheck
import com.convy.shared.domain.model.ListItem
import com.convy.shared.domain.repository.ItemRepository

class ItemRepositoryImpl(
    private val api: ConvyApi,
) : ItemRepository {

    override suspend fun getByList(listId: String, includeCompleted: Boolean): Result<List<ListItem>> =
        runCatching {
            api.getListItems(listId, includeCompleted).map { it.toDomain() }
        }

    override suspend fun create(
        listId: String,
        title: String,
        quantity: Int?,
        unit: String?,
        note: String?,
    ): Result<String> =
        runCatching {
            api.createItem(listId, CreateItemRequest(title, quantity, unit, note)).id
        }

    override suspend fun update(
        listId: String,
        itemId: String,
        title: String,
        quantity: Int?,
        unit: String?,
        note: String?,
    ): Result<Unit> =
        runCatching {
            api.updateItem(listId, itemId, UpdateItemRequest(title, quantity, unit, note))
        }

    override suspend fun delete(listId: String, itemId: String): Result<Unit> =
        runCatching {
            api.deleteItem(listId, itemId)
        }

    override suspend fun complete(listId: String, itemId: String): Result<Unit> =
        runCatching {
            api.completeItem(listId, itemId)
        }

    override suspend fun uncomplete(listId: String, itemId: String): Result<Unit> =
        runCatching {
            api.uncompleteItem(listId, itemId)
        }

    override suspend fun checkDuplicate(listId: String, title: String): Result<DuplicateCheck> =
        runCatching {
            api.checkDuplicate(listId, title).toDomain()
        }

    override suspend fun getSuggestions(householdId: String, query: String?): Result<List<String>> =
        runCatching {
            api.getItemSuggestions(householdId, query).suggestions
        }
}
