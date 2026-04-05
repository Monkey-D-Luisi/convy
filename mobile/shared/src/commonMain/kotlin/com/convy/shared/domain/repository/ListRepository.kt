package com.convy.shared.domain.repository

import com.convy.shared.domain.model.HouseholdList
import com.convy.shared.domain.model.ListType

interface ListRepository {
    suspend fun create(householdId: String, name: String, type: ListType): Result<String>
    suspend fun getByHousehold(householdId: String, includeArchived: Boolean = false): Result<List<HouseholdList>>
    suspend fun rename(householdId: String, listId: String, newName: String): Result<Unit>
    suspend fun archive(householdId: String, listId: String): Result<Unit>
}
