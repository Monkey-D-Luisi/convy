package com.convy.shared.domain.repository

import com.convy.shared.domain.model.ActivityLogEntry

interface ActivityRepository {
    suspend fun getByHousehold(householdId: String, limit: Int = 50): Result<List<ActivityLogEntry>>
}
