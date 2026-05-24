package com.convy.shared.domain.repository

import com.convy.shared.domain.model.Household

interface ActiveHouseholdRepository {
    suspend fun getActiveHouseholdId(): String?
    suspend fun setActiveHouseholdId(householdId: String)
    suspend fun clearActiveHouseholdId()
    suspend fun resolveActiveHousehold(households: List<Household>): Household?
}
