package com.convy.shared.domain.repository

import com.convy.shared.domain.model.Household
import com.convy.shared.domain.model.HouseholdDetail

interface HouseholdRepository {
    suspend fun create(name: String): Result<String>
    suspend fun getMyHouseholds(): Result<List<Household>>
    suspend fun getById(id: String): Result<HouseholdDetail>
}
