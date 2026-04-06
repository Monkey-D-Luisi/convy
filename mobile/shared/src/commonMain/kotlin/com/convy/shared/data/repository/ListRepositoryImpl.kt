package com.convy.shared.data.repository

import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.data.remote.dto.CreateListRequest
import com.convy.shared.data.remote.dto.RenameListRequest
import com.convy.shared.data.remote.toDomain
import com.convy.shared.domain.model.HouseholdList
import com.convy.shared.domain.model.ListType
import com.convy.shared.domain.repository.ListRepository

class ListRepositoryImpl(
    private val api: ConvyApi,
) : ListRepository {

    override suspend fun create(householdId: String, name: String, type: ListType): Result<String> =
        runCatching {
            val typeString = when (type) {
                ListType.Shopping -> "Shopping"
                ListType.Tasks -> "Tasks"
            }
            api.createList(householdId, CreateListRequest(name, typeString)).id
        }

    override suspend fun getByHousehold(householdId: String, includeArchived: Boolean): Result<List<HouseholdList>> =
        runCatching {
            api.getHouseholdLists(householdId, includeArchived).map { it.toDomain() }
        }

    override suspend fun rename(householdId: String, listId: String, newName: String): Result<Unit> =
        runCatching {
            api.renameList(householdId, listId, RenameListRequest(newName))
        }

    override suspend fun archive(householdId: String, listId: String): Result<Unit> =
        runCatching {
            api.archiveList(householdId, listId)
        }
}
