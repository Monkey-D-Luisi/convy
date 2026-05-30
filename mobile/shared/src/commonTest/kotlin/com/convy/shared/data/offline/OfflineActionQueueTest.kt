package com.convy.shared.data.offline

import com.convy.shared.platform.FileStorage
import kotlinx.coroutines.test.runTest
import kotlinx.serialization.json.Json
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertTrue

class OfflineActionQueueTest {
    private val json = Json { ignoreUnknownKeys = true }

    @Test
    fun `task completion actions cancel each other for the same task`() = runTest {
        val queue = OfflineActionQueue(FakeFileStorage(), json)

        queue.enqueue(
            OfflineAction.CompleteTask(
                id = "action-1",
                listId = "list-1",
                taskId = "task-1",
                createdAt = 1L,
            ),
        )
        queue.enqueue(
            OfflineAction.UncompleteTask(
                id = "action-2",
                listId = "list-1",
                taskId = "task-1",
                createdAt = 2L,
            ),
        )

        assertTrue(queue.peek().isEmpty())
    }

    @Test
    fun `task delete replaces pending task completion action`() = runTest {
        val queue = OfflineActionQueue(FakeFileStorage(), json)

        queue.enqueue(
            OfflineAction.CompleteTask(
                id = "action-1",
                listId = "list-1",
                taskId = "task-1",
                createdAt = 1L,
            ),
        )
        queue.enqueue(
            OfflineAction.DeleteTask(
                id = "action-2",
                listId = "list-1",
                taskId = "task-1",
                createdAt = 2L,
            ),
        )

        val actions = queue.peek()
        assertEquals(1, actions.size)
        assertEquals("action-2", actions.single().id)
    }

    private class FakeFileStorage : FileStorage {
        private val files = mutableMapOf<String, String>()

        override suspend fun read(filename: String): String? = files[filename]

        override suspend fun write(filename: String, content: String) {
            files[filename] = content
        }
    }
}
