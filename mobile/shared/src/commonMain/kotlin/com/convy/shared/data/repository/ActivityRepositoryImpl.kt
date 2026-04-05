package com.convy.shared.data.repository

import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.data.remote.toDomain
import com.convy.shared.domain.model.ActivityLogEntry
import com.convy.shared.domain.repository.ActivityRepository

class ActivityRepositoryImpl(
    private val api: ConvyApi
) : ActivityRepository {

    override suspend fun getByHousehold(householdId: String, limit: Int): Result<List<ActivityLogEntry>> {
        return try {
            val dtos = api.getHouseholdActivity(householdId, limit)
            Result.success(dtos.map { it.toDomain() })
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
