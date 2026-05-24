package com.convy.app.ui.screens.households

import com.convy.shared.domain.model.Household
import com.convy.shared.domain.model.HouseholdDetail
import com.convy.shared.domain.model.Invite
import com.convy.shared.domain.repository.ActiveHouseholdRepository
import com.convy.shared.domain.repository.HouseholdRepository
import com.convy.shared.domain.repository.InviteRepository
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.toList
import kotlinx.coroutines.launch
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import kotlin.test.Test
import kotlin.test.assertEquals

@OptIn(ExperimentalCoroutinesApi::class)
class HouseholdsStoreTest {
    @Test
    fun `load selects stored active household`() = runTest {
        Dispatchers.setMain(StandardTestDispatcher(testScheduler))
        try {
            val activeRepository = FakeActiveHouseholdRepository(activeHouseholdId = "home-2")
            val store = HouseholdsStore(
                initialActiveHouseholdId = null,
                householdRepository = FakeHouseholdRepository(households = mutableListOf(household("home-1"), household("home-2"))),
                inviteRepository = FakeInviteRepository(),
                activeHouseholdRepository = activeRepository,
            )

            runCurrent()

            assertEquals("home-2", store.state.value.activeHouseholdId)
            assertEquals("home-2", activeRepository.activeHouseholdId)
            store.close()
        } finally {
            Dispatchers.resetMain()
        }
    }

    @Test
    fun `select household stores active household and navigates to lists`() = runTest {
        Dispatchers.setMain(StandardTestDispatcher(testScheduler))
        try {
            val activeRepository = FakeActiveHouseholdRepository(activeHouseholdId = "home-1")
            val store = HouseholdsStore(
                initialActiveHouseholdId = null,
                householdRepository = FakeHouseholdRepository(households = mutableListOf(household("home-1"), household("home-2"))),
                inviteRepository = FakeInviteRepository(),
                activeHouseholdRepository = activeRepository,
            )
            val sideEffects = mutableListOf<HouseholdsSideEffect>()
            backgroundScope.launch(UnconfinedTestDispatcher(testScheduler)) {
                store.sideEffects.toList(sideEffects)
            }
            runCurrent()

            store.processIntent(HouseholdsIntent.SelectHousehold("home-2"))
            runCurrent()

            assertEquals("home-2", activeRepository.activeHouseholdId)
            assertEquals(HouseholdsSideEffect.NavigateToLists("home-2"), sideEffects.last())
            store.close()
        } finally {
            Dispatchers.resetMain()
        }
    }

    @Test
    fun `create household selects new household`() = runTest {
        Dispatchers.setMain(StandardTestDispatcher(testScheduler))
        try {
            val activeRepository = FakeActiveHouseholdRepository()
            val householdRepository = FakeHouseholdRepository(
                households = mutableListOf(household("home-1")),
                createdHouseholdId = "home-2",
            )
            val store = HouseholdsStore(null, householdRepository, FakeInviteRepository(), activeRepository)
            val sideEffects = mutableListOf<HouseholdsSideEffect>()
            backgroundScope.launch(UnconfinedTestDispatcher(testScheduler)) {
                store.sideEffects.toList(sideEffects)
            }
            runCurrent()

            store.processIntent(HouseholdsIntent.ShowCreateDialog)
            store.processIntent(HouseholdsIntent.UpdateNewHouseholdName("Weekend Home"))
            store.processIntent(HouseholdsIntent.CreateHousehold)
            runCurrent()

            assertEquals("home-2", activeRepository.activeHouseholdId)
            assertEquals(HouseholdsSideEffect.NavigateToLists("home-2"), sideEffects.last())
            store.close()
        } finally {
            Dispatchers.resetMain()
        }
    }

    @Test
    fun `join household selects joined household`() = runTest {
        Dispatchers.setMain(StandardTestDispatcher(testScheduler))
        try {
            val activeRepository = FakeActiveHouseholdRepository()
            val store = HouseholdsStore(
                initialActiveHouseholdId = null,
                householdRepository = FakeHouseholdRepository(households = mutableListOf(household("home-1"))),
                inviteRepository = FakeInviteRepository(joinedHouseholdId = "home-3"),
                activeHouseholdRepository = activeRepository,
            )
            val sideEffects = mutableListOf<HouseholdsSideEffect>()
            backgroundScope.launch(UnconfinedTestDispatcher(testScheduler)) {
                store.sideEffects.toList(sideEffects)
            }
            runCurrent()

            store.processIntent(HouseholdsIntent.ShowJoinDialog)
            store.processIntent(HouseholdsIntent.UpdateInviteCode("ABC12345"))
            store.processIntent(HouseholdsIntent.JoinHousehold)
            runCurrent()

            assertEquals("home-3", activeRepository.activeHouseholdId)
            assertEquals(HouseholdsSideEffect.NavigateToLists("home-3"), sideEffects.last())
            store.close()
        } finally {
            Dispatchers.resetMain()
        }
    }

    @Test
    fun `rename household updates state`() = runTest {
        Dispatchers.setMain(StandardTestDispatcher(testScheduler))
        try {
            val store = HouseholdsStore(
                initialActiveHouseholdId = "home-1",
                householdRepository = FakeHouseholdRepository(households = mutableListOf(household("home-1"))),
                inviteRepository = FakeInviteRepository(),
                activeHouseholdRepository = FakeActiveHouseholdRepository(),
            )
            runCurrent()

            store.processIntent(HouseholdsIntent.ShowRenameDialog("home-1", "Old Home"))
            store.processIntent(HouseholdsIntent.UpdateRenameText("New Home"))
            store.processIntent(HouseholdsIntent.ConfirmRename)
            runCurrent()

            assertEquals("New Home", store.state.value.households.single().name)
            store.close()
        } finally {
            Dispatchers.resetMain()
        }
    }

    @Test
    fun `leave active household selects next household`() = runTest {
        Dispatchers.setMain(StandardTestDispatcher(testScheduler))
        try {
            val activeRepository = FakeActiveHouseholdRepository(activeHouseholdId = "home-1")
            val householdRepository = FakeHouseholdRepository(
                households = mutableListOf(household("home-1"), household("home-2")),
            )
            val store = HouseholdsStore(null, householdRepository, FakeInviteRepository(), activeRepository)
            val sideEffects = mutableListOf<HouseholdsSideEffect>()
            backgroundScope.launch(UnconfinedTestDispatcher(testScheduler)) {
                store.sideEffects.toList(sideEffects)
            }
            runCurrent()

            store.processIntent(HouseholdsIntent.ShowLeaveConfirmation("home-1", "Home One"))
            store.processIntent(HouseholdsIntent.ConfirmLeaveHousehold)
            runCurrent()

            assertEquals("home-2", activeRepository.activeHouseholdId)
            assertEquals(HouseholdsSideEffect.NavigateToLists("home-2"), sideEffects.last())
            store.close()
        } finally {
            Dispatchers.resetMain()
        }
    }

    @Test
    fun `leave final household navigates to setup`() = runTest {
        Dispatchers.setMain(StandardTestDispatcher(testScheduler))
        try {
            val activeRepository = FakeActiveHouseholdRepository(activeHouseholdId = "home-1")
            val householdRepository = FakeHouseholdRepository(
                households = mutableListOf(household("home-1")),
            )
            val store = HouseholdsStore(null, householdRepository, FakeInviteRepository(), activeRepository)
            val sideEffects = mutableListOf<HouseholdsSideEffect>()
            backgroundScope.launch(UnconfinedTestDispatcher(testScheduler)) {
                store.sideEffects.toList(sideEffects)
            }
            runCurrent()

            store.processIntent(HouseholdsIntent.ShowLeaveConfirmation("home-1", "Home One"))
            store.processIntent(HouseholdsIntent.ConfirmLeaveHousehold)
            runCurrent()

            assertEquals(null, activeRepository.activeHouseholdId)
            assertEquals(HouseholdsSideEffect.NavigateToHouseholdSetup, sideEffects.last())
            store.close()
        } finally {
            Dispatchers.resetMain()
        }
    }

    private fun household(id: String, name: String = id) = Household(
        id = id,
        name = name,
        createdBy = "user-1",
        createdAt = "2026-05-23T10:00:00Z",
    )

    private class FakeActiveHouseholdRepository(
        var activeHouseholdId: String? = null,
    ) : ActiveHouseholdRepository {
        override suspend fun getActiveHouseholdId(): String? = activeHouseholdId

        override suspend fun setActiveHouseholdId(householdId: String) {
            activeHouseholdId = householdId
        }

        override suspend fun clearActiveHouseholdId() {
            activeHouseholdId = null
        }

        override suspend fun resolveActiveHousehold(households: List<Household>): Household? {
            val selected = households.firstOrNull { it.id == activeHouseholdId } ?: households.firstOrNull()
            activeHouseholdId = selected?.id
            return selected
        }
    }

    private class FakeHouseholdRepository(
        private val households: MutableList<Household>,
        private val createdHouseholdId: String = "created-home",
    ) : HouseholdRepository {
        override suspend fun create(name: String): Result<String> {
            households.add(household(createdHouseholdId, name))
            return Result.success(createdHouseholdId)
        }

        override suspend fun getMyHouseholds(): Result<List<Household>> = Result.success(households.toList())

        override suspend fun getById(id: String): Result<HouseholdDetail> =
            error("Not needed in this test")

        override suspend fun rename(id: String, newName: String): Result<Unit> {
            val index = households.indexOfFirst { it.id == id }
            if (index >= 0) {
                households[index] = households[index].copy(name = newName)
            }
            return Result.success(Unit)
        }

        override suspend fun leave(id: String): Result<Unit> {
            households.removeAll { it.id == id }
            return Result.success(Unit)
        }

        private fun household(id: String, name: String = id) = Household(
            id = id,
            name = name,
            createdBy = "user-1",
            createdAt = "2026-05-23T10:00:00Z",
        )
    }

    private class FakeInviteRepository(
        private val joinedHouseholdId: String = "joined-home",
    ) : InviteRepository {
        override suspend fun create(householdId: String): Result<Invite> =
            error("Not needed in this test")

        override suspend fun join(inviteCode: String): Result<String> = Result.success(joinedHouseholdId)

        override suspend fun getByHousehold(householdId: String): Result<List<Invite>> =
            error("Not needed in this test")

        override suspend fun revoke(inviteId: String): Result<Unit> =
            error("Not needed in this test")
    }
}
