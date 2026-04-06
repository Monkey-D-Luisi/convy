package com.convy.shared.data.repository

import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.data.remote.dto.CreateHouseholdRequest
import com.convy.shared.data.remote.dto.RenameHouseholdRequest
import com.convy.shared.data.remote.toDomain
import com.convy.shared.domain.model.Household
import com.convy.shared.domain.model.HouseholdDetail
import com.convy.shared.domain.repository.HouseholdRepository

class HouseholdRepositoryImpl(
    private val api: ConvyApi,
) : HouseholdRepository {

    override suspend fun create(name: String): Result<String> =
        runCatching {
            api.createHousehold(CreateHouseholdRequest(name)).id
        }

    override suspend fun getMyHouseholds(): Result<List<Household>> =
        runCatching {
            api.getMyHouseholds().map { it.toDomain() }
        }

    override suspend fun getById(id: String): Result<HouseholdDetail> =
        runCatching {
            api.getHousehold(id).toDomain()
        }

    override suspend fun rename(id: String, newName: String): Result<Unit> =
        runCatching {
            api.renameHousehold(id, RenameHouseholdRequest(newName))
        }

    override suspend fun leave(id: String): Result<Unit> =
        runCatching {
            api.leaveHousehold(id)
        }
}
