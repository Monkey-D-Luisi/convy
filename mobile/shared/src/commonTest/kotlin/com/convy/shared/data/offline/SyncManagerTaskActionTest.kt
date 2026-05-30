package com.convy.shared.data.offline

import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.platform.FileStorage
import com.convy.shared.platform.NetworkMonitor
import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.respond
import io.ktor.client.plugins.defaultRequest
import io.ktor.http.HttpStatusCode
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.test.runTest
import kotlinx.serialization.json.Json
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertTrue

class SyncManagerTaskActionTest {
    private val json = Json { ignoreUnknownKeys = true }

    @Test
    fun `flush executes queued task actions`() = runTest {
        val queue = OfflineActionQueue(FakeFileStorage(), json)
        queue.enqueue(OfflineAction.CompleteTask("action-1", "list-1", "task-1", createdAt = 1L))
        queue.enqueue(OfflineAction.UncompleteTask("action-2", "list-1", "task-2", createdAt = 2L))
        queue.enqueue(OfflineAction.DeleteTask("action-3", "list-1", "task-3", createdAt = 3L))

        val paths = mutableListOf<String>()
        val api = ConvyApi(
            HttpClient(
                MockEngine { request ->
                    paths.add(request.url.encodedPath)
                    respond("", HttpStatusCode.OK)
                },
            ) {
                defaultRequest { url("https://example.test/") }
            },
        )
        val syncManager = SyncManager(queue, FakeNetworkMonitor(), api)

        syncManager.flush()

        assertEquals(
            listOf(
                "/api/v1/lists/list-1/tasks/task-1/complete",
                "/api/v1/lists/list-1/tasks/task-2/uncomplete",
                "/api/v1/lists/list-1/tasks/task-3",
            ),
            paths,
        )
        assertTrue(queue.peek().isEmpty())
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
}
