package com.convy.shared.data.offline

import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.platform.NetworkMonitor
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import kotlin.coroutines.cancellation.CancellationException

class SyncManager(
    private val queue: OfflineActionQueue,
    private val networkMonitor: NetworkMonitor,
    private val api: ConvyApi,
) {
    private val scope = CoroutineScope(Dispatchers.Default + SupervisorJob())
    private val _isSyncing = MutableStateFlow(false)
    val isSyncing: StateFlow<Boolean> = _isSyncing.asStateFlow()

    fun start() {
        scope.launch { queue.load() }

        // Observe connectivity changes and flush when online
        scope.launch {
            networkMonitor.isOnline
                .collect { online ->
                    if (online) {
                        flush()
                    }
                }
        }
    }

    suspend fun flush() {
        if (_isSyncing.value) return
        _isSyncing.value = true

        try {
            while (true) {
                val actions = queue.peek()
                if (actions.isEmpty()) break

                val action = actions.first()
                val success = executeAction(action)

                if (success) {
                    queue.dequeue(action.id)
                } else if (action.retryCount >= MAX_RETRIES) {
                    // Discard after max retries
                    queue.dequeue(action.id)
                } else {
                    // Update retry count and delay with exponential backoff
                    queue.updateAction(action.withIncrementedRetry())
                    val backoffMs = calculateBackoff(action.retryCount)
                    delay(backoffMs)
                }
            }
        } finally {
            _isSyncing.value = false
        }
    }

    private suspend fun executeAction(action: OfflineAction): Boolean {
        return try {
            when (action) {
                is OfflineAction.CompleteItem -> {
                    api.completeItem(action.listId, action.itemId)
                    true
                }
                is OfflineAction.UncompleteItem -> {
                    api.uncompleteItem(action.listId, action.itemId)
                    true
                }
                is OfflineAction.DeleteItem -> {
                    api.deleteItem(action.listId, action.itemId)
                    true
                }
                is OfflineAction.CompleteTask -> {
                    api.completeTask(action.listId, action.taskId)
                    true
                }
                is OfflineAction.UncompleteTask -> {
                    api.uncompleteTask(action.listId, action.taskId)
                    true
                }
                is OfflineAction.DeleteTask -> {
                    api.deleteTask(action.listId, action.taskId)
                    true
                }
            }
        } catch (e: CancellationException) {
            throw e
        } catch (_: Exception) {
            false
        }
    }

    private fun calculateBackoff(retryCount: Int): Long {
        val baseMs = 1000L
        val maxMs = 30_000L
        val backoff = baseMs * (1L shl retryCount.coerceAtMost(5))
        return backoff.coerceAtMost(maxMs)
    }

    companion object {
        private const val MAX_RETRIES = 5
    }
}
