package com.convy.shared.data.repository

import com.convy.shared.domain.model.Household
import com.convy.shared.domain.repository.ActiveHouseholdRepository
import com.convy.shared.platform.FileStorage

class ActiveHouseholdRepositoryImpl(
    private val fileStorage: FileStorage,
) : ActiveHouseholdRepository {

    override suspend fun getActiveHouseholdId(): String? =
        fileStorage.read(FILENAME)?.trim()?.takeIf { it.isNotEmpty() }

    override suspend fun setActiveHouseholdId(householdId: String) {
        fileStorage.write(FILENAME, householdId)
    }

    override suspend fun clearActiveHouseholdId() {
        fileStorage.write(FILENAME, "")
    }

    override suspend fun resolveActiveHousehold(households: List<Household>): Household? {
        val activeHouseholdId = getActiveHouseholdId()
        val selected = households.firstOrNull { it.id == activeHouseholdId } ?: households.firstOrNull()

        if (selected == null) {
            clearActiveHouseholdId()
        } else if (selected.id != activeHouseholdId) {
            setActiveHouseholdId(selected.id)
        }

        return selected
    }

    private companion object {
        const val FILENAME = "active_household_id.txt"
    }
}
