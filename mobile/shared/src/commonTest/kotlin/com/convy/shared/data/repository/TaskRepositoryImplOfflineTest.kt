package com.convy.shared.data.repository

import com.convy.shared.data.offline.OfflineAction
import com.convy.shared.data.offline.OfflineActionQueue
import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.platform.FileStorage
import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.plugins.defaultRequest
import io.ktor.utils.io.errors.IOException
import kotlinx.coroutines.test.runTest
import kotlinx.serialization.json.Json
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertIs
import kotlin.test.assertTrue

class TaskRepositoryImplOfflineTest {
    private val json = Json { ignoreUnknownKeys = true }

    @Test
    fun `complete queues task action when network is unavailable`() = runTest {
        val queue = OfflineActionQueue(FakeFileStorage(), json)
        val repository = TaskRepositoryImpl(
            api = ConvyApi(
                HttpClient(MockEngine { throw IOException("offline") }) {
                    defaultRequest { url("https://example.test/") }
                },
            ),
            offlineQueue = queue,
        )

        val result = repository.complete("list-1", "task-1")

        assertTrue(result.isSuccess)
        val action = assertIs<OfflineAction.CompleteTask>(queue.peek().single())
        assertEquals("list-1", action.listId)
        assertEquals("task-1", action.taskId)
    }

    private class FakeFileStorage : FileStorage {
        private val files = mutableMapOf<String, String>()

        override suspend fun read(filename: String): String? = files[filename]

        override suspend fun write(filename: String, content: String) {
            files[filename] = content
        }
    }
}
