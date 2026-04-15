package com.convy.shared.data.offline

import com.convy.shared.platform.FileStorage
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

class OfflineActionQueue(
    private val fileStorage: FileStorage,
    private val json: Json,
) {
    private val mutex = Mutex()
    private val _actions = MutableStateFlow<List<OfflineAction>>(emptyList())
    val actions: StateFlow<List<OfflineAction>> = _actions.asStateFlow()

    suspend fun load() {
        mutex.withLock {
            val content = fileStorage.read(FILENAME)
            if (content != null) {
                _actions.value = try {
                    json.decodeFromString<List<OfflineAction>>(content)
                } catch (_: Exception) {
                    emptyList()
                }
            }
        }
    }

    suspend fun enqueue(action: OfflineAction) {
        mutex.withLock {
            val current = _actions.value.toMutableList()

            // Collapse logic: if there's already an action for this itemId, handle it
            val existingIndex = current.indexOfFirst { it.itemId == action.itemId }
            if (existingIndex >= 0) {
                val existing = current[existingIndex]
                // If the new action cancels the existing one (complete ↔ uncomplete), remove both
                val cancelsOut = (existing is OfflineAction.CompleteItem && action is OfflineAction.UncompleteItem) ||
                    (existing is OfflineAction.UncompleteItem && action is OfflineAction.CompleteItem)
                if (cancelsOut) {
                    current.removeAt(existingIndex)
                    _actions.value = current
                    persist(current)
                    return
                }
                // Otherwise replace (e.g., a new toggle supersedes)
                current[existingIndex] = action
            } else {
                current.add(action)
            }

            _actions.value = current
            persist(current)
        }
    }

    suspend fun dequeue(actionId: String) {
        mutex.withLock {
            val current = _actions.value.filter { it.id != actionId }
            _actions.value = current
            persist(current)
        }
    }

    suspend fun updateAction(updated: OfflineAction) {
        mutex.withLock {
            val current = _actions.value.map { if (it.id == updated.id) updated else it }
            _actions.value = current
            persist(current)
        }
    }

    suspend fun peek(): List<OfflineAction> = mutex.withLock { _actions.value }

    private suspend fun persist(actions: List<OfflineAction>) {
        fileStorage.write(FILENAME, json.encodeToString(actions))
    }

    companion object {
        private const val FILENAME = "offline_queue.json"
    }
}
