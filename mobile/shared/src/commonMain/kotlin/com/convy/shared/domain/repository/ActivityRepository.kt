package com.convy.shared.domain.repository

import com.convy.shared.domain.model.ActivityLogEntry

interface ActivityRepository {
    suspend fun getByHousehold(householdId: String, limit: Int = 50, before: String? = null): Result<List<ActivityLogEntry>>
    suspend fun getItemHistory(itemId: String): Result<List<ActivityLogEntry>>
}
