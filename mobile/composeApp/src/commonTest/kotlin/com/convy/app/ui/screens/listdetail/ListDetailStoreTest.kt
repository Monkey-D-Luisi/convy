package com.convy.app.ui.screens.listdetail

import com.convy.shared.config.ApiConfig
import com.convy.shared.data.offline.OfflineActionQueue
import com.convy.shared.data.remote.HouseholdRealtimeService
import com.convy.shared.data.remote.SignalRClient
import com.convy.shared.data.remote.TokenProvider
import com.convy.shared.domain.model.DuplicateCheck
import com.convy.shared.domain.model.ListItem
import com.convy.shared.domain.model.ParsedItem
import com.convy.shared.domain.model.ParsedTask
import com.convy.shared.domain.model.TaskItem
import com.convy.shared.domain.model.TaskPriority
import com.convy.shared.domain.model.TaskVoiceParseResult
import com.convy.shared.domain.model.User
import com.convy.shared.domain.model.VoiceParseResult
import com.convy.shared.domain.repository.AuthRepository
import com.convy.shared.domain.repository.ItemRepository
import com.convy.shared.domain.repository.TaskRepository
import com.convy.shared.platform.AudioRecorder
import com.convy.shared.platform.FileStorage
import com.convy.shared.platform.NetworkMonitor
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.toList
import kotlinx.coroutines.launch
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import kotlinx.serialization.json.Json
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertTrue

@OptIn(ExperimentalCoroutinesApi::class)
class ListDetailStoreTest {
    @Test
    fun `pending item delete is committed before navigating away`() = runTest {
        Dispatchers.setMain(StandardTestDispatcher(testScheduler))
        try {
            val itemRepository = FakeItemRepository(items = mutableListOf(listItem(id = "item-1")))
            val store = createStore(itemRepository = itemRepository)
            val sideEffects = mutableListOf<ListDetailSideEffect>()
            backgroundScope.launch(UnconfinedTestDispatcher(testScheduler)) {
                store.sideEffects.toList(sideEffects)
            }
            runCurrent()

            store.processIntent(ListDetailIntent.DeleteItem("item-1"))
            runCurrent()
            store.processIntent(ListDetailIntent.NavigateBack)
            runCurrent()

            assertEquals(listOf("item-1"), itemRepository.deletedItemIds)
            assertTrue(sideEffects.any { it is ListDetailSideEffect.NavigateBack })
            store.close()
        } finally {
            Dispatchers.resetMain()
        }
    }

    @Test
    fun `pending task delete is committed before navigating away`() = runTest {
        Dispatchers.setMain(StandardTestDispatcher(testScheduler))
        try {
            val taskRepository = FakeTaskRepository(tasks = mutableListOf(taskItem(id = "task-1")))
            val store = createStore(taskRepository = taskRepository, listType = "Tasks")
            val sideEffects = mutableListOf<ListDetailSideEffect>()
            backgroundScope.launch(UnconfinedTestDispatcher(testScheduler)) {
                store.sideEffects.toList(sideEffects)
            }
            runCurrent()

            store.processIntent(ListDetailIntent.DeleteItem("task-1"))
            runCurrent()
            store.processIntent(ListDetailIntent.NavigateBack)
            runCurrent()

            assertEquals(listOf("task-1"), taskRepository.deletedTaskIds)
            assertTrue(sideEffects.any { it is ListDetailSideEffect.NavigateBack })
            store.close()
        } finally {
            Dispatchers.resetMain()
        }
    }

    private fun createStore(
        itemRepository: ItemRepository = FakeItemRepository(),
        taskRepository: TaskRepository = FakeTaskRepository(),
        listType: String = "Shopping",
    ): ListDetailStore {
        val json = Json { ignoreUnknownKeys = true }
        val signalRClient = SignalRClient(
            tokenProvider = object : TokenProvider {
                override suspend fun getToken(): String? = null
            },
            json = json,
            apiConfig = ApiConfig(protocol = "http", host = "127.0.0.1", port = 1),
        )

        return ListDetailStore(
            householdId = "home-1",
            listId = "list-1",
            listName = "Groceries",
            listType = listType,
            itemRepository = itemRepository,
            taskRepository = taskRepository,
            realtimeService = HouseholdRealtimeService(signalRClient, json),
            audioRecorder = FakeAudioRecorder(),
            networkMonitor = FakeNetworkMonitor(),
            offlineQueue = OfflineActionQueue(FakeFileStorage(), json),
            signalRClient = signalRClient,
            authRepository = FakeAuthRepository(),
        )
    }

    private fun listItem(id: String) = ListItem(
        id = id,
        title = "Milk",
        quantity = null,
        unit = null,
        note = null,
        listId = "list-1",
        createdBy = "user-1",
        createdByName = "Luis",
        createdAt = "2026-05-29T10:00:00Z",
        isCompleted = false,
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

    private fun taskItem(id: String) = TaskItem(
        id = id,
        title = "Clean kitchen",
        note = null,
        listId = "list-1",
        createdBy = "user-1",
        createdByName = "Luis",
        createdAt = "2026-05-29T10:00:00Z",
        isCompleted = false,
        completedBy = null,
        completedByName = null,
        completedAt = null,
    )

    private class FakeItemRepository(
        private val items: MutableList<ListItem> = mutableListOf(),
    ) : ItemRepository {
        val deletedItemIds = mutableListOf<String>()

        override suspend fun getByList(listId: String, status: String?, createdBy: String?): Result<List<ListItem>> =
            Result.success(items.toList())

        override suspend fun create(
            listId: String,
            title: String,
            quantity: Int?,
            unit: String?,
            note: String?,
            recurrenceFrequency: Int?,
            recurrenceInterval: Int?,
        ): Result<String> = error("Not needed in this test")

        override suspend fun update(
            listId: String,
            itemId: String,
            title: String,
            quantity: Int?,
            unit: String?,
            note: String?,
            recurrenceFrequency: Int?,
            recurrenceInterval: Int?,
        ): Result<Unit> = error("Not needed in this test")

        override suspend fun delete(listId: String, itemId: String): Result<Unit> {
            deletedItemIds.add(itemId)
            items.removeAll { it.id == itemId }
            return Result.success(Unit)
        }

        override suspend fun complete(listId: String, itemId: String): Result<Unit> =
            error("Not needed in this test")

        override suspend fun uncomplete(listId: String, itemId: String): Result<Unit> =
            error("Not needed in this test")

        override suspend fun checkDuplicate(listId: String, title: String): Result<DuplicateCheck> =
            error("Not needed in this test")

        override suspend fun getSuggestions(householdId: String, query: String?): Result<List<String>> =
            error("Not needed in this test")

        override suspend fun parseVoiceAudio(listId: String, audioData: ByteArray): Result<VoiceParseResult> =
            error("Not needed in this test")

        override suspend fun batchCreate(listId: String, items: List<ParsedItem>, source: String): Result<List<String>> =
            error("Not needed in this test")
    }

    private class FakeTaskRepository(
        private val tasks: MutableList<TaskItem> = mutableListOf(),
    ) : TaskRepository {
        val deletedTaskIds = mutableListOf<String>()

        override suspend fun getByList(listId: String, status: String?, createdBy: String?): Result<List<TaskItem>> =
            Result.success(tasks.toList())

        override suspend fun create(
            listId: String,
            title: String,
            note: String?,
            assignedToUserId: String?,
            dueDate: String?,
            reminderAtUtc: String?,
            priority: TaskPriority,
        ): Result<String> =
            error("Not needed in this test")

        override suspend fun update(
            listId: String,
            taskId: String,
            title: String,
            note: String?,
            assignedToUserId: String?,
            dueDate: String?,
            reminderAtUtc: String?,
            priority: TaskPriority,
        ): Result<Unit> =
            error("Not needed in this test")

        override suspend fun delete(listId: String, taskId: String): Result<Unit> {
            deletedTaskIds.add(taskId)
            tasks.removeAll { it.id == taskId }
            return Result.success(Unit)
        }

        override suspend fun complete(listId: String, taskId: String): Result<Unit> =
            error("Not needed in this test")

        override suspend fun uncomplete(listId: String, taskId: String): Result<Unit> =
            error("Not needed in this test")

        override suspend fun parseVoiceAudio(listId: String, audioData: ByteArray): Result<TaskVoiceParseResult> =
            error("Not needed in this test")

        override suspend fun batchCreate(listId: String, tasks: List<ParsedTask>): Result<Unit> =
            error("Not needed in this test")
    }

    private class FakeAudioRecorder : AudioRecorder {
        override fun startRecording() = Unit
        override fun stopRecording(): ByteArray? = null
        override fun isRecording(): Boolean = false
        override fun release() = Unit
    }

    private class FakeNetworkMonitor : NetworkMonitor {
        override val isOnline = MutableStateFlow(true)
        override fun isCurrentlyOnline(): Boolean = true
    }

    private class FakeFileStorage : FileStorage {
        private val files = mutableMapOf<String, String>()
        override suspend fun read(filename: String): String? = files[filename]
        override suspend fun write(filename: String, content: String) {
            files[filename] = content
        }
    }

    private class FakeAuthRepository : AuthRepository {
        override suspend fun signIn(email: String, password: String): Result<User> =
            error("Not needed in this test")

        override suspend fun signUp(email: String, password: String, displayName: String): Result<User> =
            error("Not needed in this test")

        override suspend fun signInWithGoogle(): Result<User> =
            error("Not needed in this test")

        override suspend fun signOut() = Unit

        override suspend fun getCurrentUser(): User? = User(
            id = "user-1",
            displayName = "Luis",
            email = "luis@example.com",
            createdAt = "2026-05-29T09:00:00Z",
        )

        override suspend fun getIdToken(): String? = null
    }
}
