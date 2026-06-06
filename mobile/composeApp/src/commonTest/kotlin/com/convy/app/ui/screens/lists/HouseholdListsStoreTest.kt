package com.convy.app.ui.screens.lists

import com.convy.shared.config.ApiConfig
import com.convy.shared.data.remote.HouseholdEvent
import com.convy.shared.data.remote.HouseholdRealtimeService
import com.convy.shared.data.remote.SignalRClient
import com.convy.shared.data.remote.TokenProvider
import com.convy.shared.data.remote.dto.ListItemDto
import com.convy.shared.data.remote.dto.TaskItemDto
import com.convy.shared.domain.model.DuplicateCheck
import com.convy.shared.domain.model.Household
import com.convy.shared.domain.model.HouseholdDetail
import com.convy.shared.domain.model.HouseholdList
import com.convy.shared.domain.model.ListItem
import com.convy.shared.domain.model.ListType
import com.convy.shared.domain.model.ParsedItem
import com.convy.shared.domain.model.ParsedTask
import com.convy.shared.domain.model.TaskItem
import com.convy.shared.domain.model.TaskPriority
import com.convy.shared.domain.model.TaskVoiceParseResult
import com.convy.shared.domain.model.VoiceParseResult
import com.convy.shared.domain.repository.ActiveHouseholdRepository
import com.convy.shared.domain.repository.HouseholdRepository
import com.convy.shared.domain.repository.ItemRepository
import com.convy.shared.domain.repository.ListRepository
import com.convy.shared.domain.repository.TaskRepository
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.advanceUntilIdle
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import kotlinx.serialization.json.Json
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertTrue

@OptIn(ExperimentalCoroutinesApi::class)
class HouseholdListsStoreTest {
    @Test
    fun `loadData counts pending shopping items and pending tasks by list type`() = runTest {
        Dispatchers.setMain(StandardTestDispatcher(testScheduler))
        try {
            val lists = listOf(
                householdList(id = "shopping-1", type = ListType.Shopping),
                householdList(id = "tasks-1", type = ListType.Tasks),
            )
            val itemRepository = FakeItemRepository(
                itemsByList = mapOf(
                    "shopping-1" to listOf(
                        listItem(id = "item-1", listId = "shopping-1", isCompleted = false),
                        listItem(id = "item-2", listId = "shopping-1", isCompleted = true),
                    ),
                ),
            )
            val taskRepository = FakeTaskRepository(
                tasksByList = mapOf(
                    "tasks-1" to listOf(
                        taskItem(id = "task-1", listId = "tasks-1", isCompleted = false),
                        taskItem(id = "task-2", listId = "tasks-1", isCompleted = false),
                        taskItem(id = "task-3", listId = "tasks-1", isCompleted = true),
                    ),
                ),
            )
            val store = createStore(
                lists = lists,
                itemRepository = itemRepository,
                taskRepository = taskRepository,
            )

            advanceUntilIdle()

            assertEquals(1, store.state.value.pendingCounts["shopping-1"])
            assertEquals(2, store.state.value.pendingCounts["tasks-1"])
            assertEquals(listOf<Pair<String, String?>>("shopping-1" to "Pending"), itemRepository.getByListCalls)
            assertEquals(listOf<Pair<String, String?>>("tasks-1" to "Pending"), taskRepository.getByListCalls)
            store.close()
        } finally {
            Dispatchers.resetMain()
        }
    }

    @Test
    fun `realtime item and task events refresh household list counts`() {
        assertTrue(shouldRefreshPendingCountsForEvent(HouseholdEvent.ItemCreated(listItemDto())))
        assertTrue(shouldRefreshPendingCountsForEvent(HouseholdEvent.ItemCompleted(listItemDto())))
        assertTrue(shouldRefreshPendingCountsForEvent(HouseholdEvent.TaskCreated(taskItemDto())))
        assertTrue(shouldRefreshPendingCountsForEvent(HouseholdEvent.TaskCompleted(taskItemDto())))
        assertFalse(shouldRefreshPendingCountsForEvent(HouseholdEvent.MemberJoined("user-2", "Alex")))
    }

    private fun createStore(
        lists: List<HouseholdList>,
        itemRepository: ItemRepository = FakeItemRepository(),
        taskRepository: TaskRepository = FakeTaskRepository(),
    ): HouseholdListsStore {
        val json = Json { ignoreUnknownKeys = true }
        val signalRClient = SignalRClient(
            tokenProvider = object : TokenProvider {
                override suspend fun getToken(): String? = null
            },
            json = json,
            apiConfig = ApiConfig(protocol = "http", host = "127.0.0.1", port = 1),
        )

        return HouseholdListsStore(
            householdId = "home-1",
            householdRepository = FakeHouseholdRepository(),
            listRepository = FakeListRepository(lists),
            itemRepository = itemRepository,
            taskRepository = taskRepository,
            realtimeService = HouseholdRealtimeService(signalRClient, json),
            activeHouseholdRepository = FakeActiveHouseholdRepository(),
        )
    }

    private fun householdList(id: String, type: ListType) = HouseholdList(
        id = id,
        name = if (type == ListType.Tasks) "Chores" else "Groceries",
        type = type,
        householdId = "home-1",
        createdBy = "user-1",
        createdAt = "2026-06-03T10:00:00Z",
        isArchived = false,
        archivedAt = null,
    )

    private fun listItem(id: String, listId: String, isCompleted: Boolean) = ListItem(
        id = id,
        title = "Milk",
        quantity = null,
        unit = null,
        note = null,
        listId = listId,
        createdBy = "user-1",
        createdByName = "Luis",
        createdAt = "2026-06-03T10:00:00Z",
        isCompleted = isCompleted,
        completedBy = null,
        completedByName = null,
        completedAt = null,
        returnedToPendingBy = null,
        returnedToPendingByName = null,
        returnedToPendingAt = null,
        recurrenceFrequency = null,
        recurrenceInterval = null,
        nextDueDate = null,
    )

    private fun taskItem(id: String, listId: String, isCompleted: Boolean) = TaskItem(
        id = id,
        title = "Clean kitchen",
        note = null,
        listId = listId,
        createdBy = "user-1",
        createdByName = "Luis",
        createdAt = "2026-06-03T10:00:00Z",
        isCompleted = isCompleted,
        completedBy = null,
        completedByName = null,
        completedAt = null,
    )

    private fun listItemDto() = ListItemDto(
        id = "item-1",
        title = "Milk",
        quantity = null,
        unit = null,
        note = null,
        listId = "shopping-1",
        createdBy = "user-1",
        createdByName = "Luis",
        createdAt = "2026-06-03T10:00:00Z",
        isCompleted = false,
        completedBy = null,
        completedByName = null,
        completedAt = null,
    )

    private fun taskItemDto() = TaskItemDto(
        id = "task-1",
        title = "Clean kitchen",
        note = null,
        listId = "tasks-1",
        createdBy = "user-1",
        createdByName = "Luis",
        createdAt = "2026-06-03T10:00:00Z",
        isCompleted = false,
        completedBy = null,
        completedByName = null,
        completedAt = null,
    )

    private class FakeHouseholdRepository : HouseholdRepository {
        override suspend fun create(name: String): Result<String> = Result.success("home-1")

        override suspend fun getMyHouseholds(): Result<List<Household>> =
            Result.success(listOf(Household("home-1", "Home", "user-1", "2026-06-03T10:00:00Z")))

        override suspend fun getById(id: String): Result<HouseholdDetail> =
            Result.success(HouseholdDetail(id, "Home", "user-1", "2026-06-03T10:00:00Z", emptyList()))

        override suspend fun rename(id: String, newName: String): Result<Unit> = Result.success(Unit)

        override suspend fun leave(id: String): Result<Unit> = Result.success(Unit)
    }

    private class FakeListRepository(
        private val lists: List<HouseholdList>,
    ) : ListRepository {
        override suspend fun create(householdId: String, name: String, type: ListType): Result<String> =
            Result.success("list-new")

        override suspend fun getByHousehold(householdId: String, includeArchived: Boolean): Result<List<HouseholdList>> =
            Result.success(lists)

        override suspend fun rename(householdId: String, listId: String, newName: String): Result<Unit> =
            Result.success(Unit)

        override suspend fun archive(householdId: String, listId: String): Result<Unit> =
            Result.success(Unit)
    }

    private class FakeActiveHouseholdRepository : ActiveHouseholdRepository {
        private var activeHouseholdId: String? = null

        override suspend fun getActiveHouseholdId(): String? = activeHouseholdId

        override suspend fun setActiveHouseholdId(householdId: String) {
            activeHouseholdId = householdId
        }

        override suspend fun clearActiveHouseholdId() {
            activeHouseholdId = null
        }

        override suspend fun resolveActiveHousehold(households: List<Household>): Household? =
            households.firstOrNull { it.id == activeHouseholdId } ?: households.firstOrNull()
    }

    private class FakeItemRepository(
        private val itemsByList: Map<String, List<ListItem>> = emptyMap(),
    ) : ItemRepository {
        val getByListCalls = mutableListOf<Pair<String, String?>>()

        override suspend fun getByList(listId: String, status: String?, createdBy: String?): Result<List<ListItem>> {
            getByListCalls += listId to status
            return Result.success(itemsByList[listId].orEmpty().filterItemsByStatus(status))
        }

        override suspend fun create(
            listId: String,
            title: String,
            quantity: Int?,
            unit: String?,
            note: String?,
            recurrenceFrequency: Int?,
            recurrenceInterval: Int?,
        ): Result<String> = Result.success("item-new")

        override suspend fun update(
            listId: String,
            itemId: String,
            title: String,
            quantity: Int?,
            unit: String?,
            note: String?,
            recurrenceFrequency: Int?,
            recurrenceInterval: Int?,
        ): Result<Unit> = Result.success(Unit)

        override suspend fun delete(listId: String, itemId: String): Result<Unit> = Result.success(Unit)

        override suspend fun complete(listId: String, itemId: String): Result<Unit> = Result.success(Unit)

        override suspend fun uncomplete(listId: String, itemId: String): Result<Unit> = Result.success(Unit)

        override suspend fun checkDuplicate(listId: String, title: String): Result<DuplicateCheck> =
            Result.success(DuplicateCheck(false, emptyList()))

        override suspend fun getSuggestions(householdId: String, query: String?): Result<List<String>> =
            Result.success(emptyList())

        override suspend fun parseVoiceAudio(listId: String, audioData: ByteArray): Result<VoiceParseResult> =
            Result.success(VoiceParseResult("", emptyList()))

        override suspend fun batchCreate(listId: String, items: List<ParsedItem>, source: String): Result<List<String>> =
            Result.success(emptyList())
    }

    private class FakeTaskRepository(
        private val tasksByList: Map<String, List<TaskItem>> = emptyMap(),
    ) : TaskRepository {
        val getByListCalls = mutableListOf<Pair<String, String?>>()

        override suspend fun getByList(listId: String, status: String?, createdBy: String?): Result<List<TaskItem>> {
            getByListCalls += listId to status
            return Result.success(tasksByList[listId].orEmpty().filterTasksByStatus(status))
        }

        override suspend fun create(
            listId: String,
            title: String,
            note: String?,
            assignedToUserId: String?,
            dueDate: String?,
            reminderAtUtc: String?,
            priority: TaskPriority,
        ): Result<String> = Result.success("task-new")

        override suspend fun update(
            listId: String,
            taskId: String,
            title: String,
            note: String?,
            assignedToUserId: String?,
            dueDate: String?,
            reminderAtUtc: String?,
            priority: TaskPriority,
        ): Result<Unit> = Result.success(Unit)

        override suspend fun delete(listId: String, taskId: String): Result<Unit> = Result.success(Unit)

        override suspend fun complete(listId: String, taskId: String): Result<Unit> = Result.success(Unit)

        override suspend fun uncomplete(listId: String, taskId: String): Result<Unit> = Result.success(Unit)

        override suspend fun parseVoiceAudio(listId: String, audioData: ByteArray): Result<TaskVoiceParseResult> =
            Result.success(TaskVoiceParseResult("", emptyList()))

        override suspend fun batchCreate(listId: String, tasks: List<ParsedTask>): Result<Unit> =
            Result.success(Unit)
    }
}

private fun List<ListItem>.filterItemsByStatus(status: String?): List<ListItem> =
    when (status) {
        "Pending" -> filter { !it.isCompleted }
        "Completed" -> filter { it.isCompleted }
        else -> this
    }

private fun List<TaskItem>.filterTasksByStatus(status: String?): List<TaskItem> =
    when (status) {
        "Pending" -> filter { !it.isCompleted }
        "Completed" -> filter { it.isCompleted }
        else -> this
    }
