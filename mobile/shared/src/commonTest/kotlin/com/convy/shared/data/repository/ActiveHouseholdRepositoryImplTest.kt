package com.convy.shared.data.repository

import com.convy.shared.domain.model.Household
import com.convy.shared.platform.FileStorage
import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class ActiveHouseholdRepositoryImplTest {
    @Test
    fun `resolve active household uses stored household when it still exists`() = runTest {
        val repository = ActiveHouseholdRepositoryImpl(FakeFileStorage())
        repository.setActiveHouseholdId("home-2")

        val selected = repository.resolveActiveHousehold(listOf(household("home-1"), household("home-2")))

        assertEquals("home-2", selected?.id)
        assertEquals("home-2", repository.getActiveHouseholdId())
    }

    @Test
    fun `resolve active household falls back to first household when stored household is stale`() = runTest {
        val repository = ActiveHouseholdRepositoryImpl(FakeFileStorage())
        repository.setActiveHouseholdId("old-home")

        val selected = repository.resolveActiveHousehold(listOf(household("home-1"), household("home-2")))

        assertEquals("home-1", selected?.id)
        assertEquals("home-1", repository.getActiveHouseholdId())
    }

    @Test
    fun `resolve active household clears stored household when no households exist`() = runTest {
        val repository = ActiveHouseholdRepositoryImpl(FakeFileStorage())
        repository.setActiveHouseholdId("home-1")

        val selected = repository.resolveActiveHousehold(emptyList())

        assertNull(selected)
        assertNull(repository.getActiveHouseholdId())
    }

    private fun household(id: String) = Household(
        id = id,
        name = "Home $id",
        createdBy = "user-1",
        createdAt = "2026-05-23T10:00:00Z",
    )

    private class FakeFileStorage : FileStorage {
        private val files = mutableMapOf<String, String>()

        override suspend fun read(filename: String): String? = files[filename]

        override suspend fun write(filename: String, content: String) {
            files[filename] = content
        }
    }
}
