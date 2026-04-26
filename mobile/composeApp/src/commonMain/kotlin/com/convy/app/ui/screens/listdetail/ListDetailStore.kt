package com.convy.app.ui.screens.listdetail

import com.convy.app.generated.resources.*
import com.convy.app.util.UiText
import com.convy.shared.data.offline.OfflineActionQueue
import com.convy.shared.data.remote.ConnectionState
import com.convy.shared.data.remote.HouseholdEvent
import com.convy.shared.data.remote.HouseholdRealtimeService
import com.convy.shared.data.remote.SignalRClient
import com.convy.shared.domain.repository.ItemRepository
import com.convy.shared.domain.repository.TaskRepository
import com.convy.shared.platform.AudioRecorder
import com.convy.shared.platform.NetworkMonitor
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.collect
import kotlinx.coroutines.flow.distinctUntilChanged
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.datetime.Clock
import org.jetbrains.compose.resources.StringResource

class ListDetailStore(
    private val householdId: String,
    private val listId: String,
    private val listName: String,
    private val listType: String,
    private val itemRepository: ItemRepository,
    private val taskRepository: TaskRepository,
    private val realtimeService: HouseholdRealtimeService,
    private val audioRecorder: AudioRecorder,
    private val networkMonitor: NetworkMonitor,
    private val offlineQueue: OfflineActionQueue,
    private val signalRClient: SignalRClient,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val isTaskList = listType.equals("Tasks", ignoreCase = true)
    private var recordingStartTime: Long = 0L
    private var nextOperationId = 1L
    private val operations = mutableMapOf<Long, EntryOperation>()
    private val redoOperations = mutableMapOf<Long, EntryOperation>()

    private val _state = MutableStateFlow(
        ListDetailState(
            listId = listId,
            householdId = householdId,
            listName = listName,
            listType = listType,
        ),
    )
    val state: StateFlow<ListDetailState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<ListDetailSideEffect>()
    val sideEffects: SharedFlow<ListDetailSideEffect> = _sideEffects.asSharedFlow()

    init {
        loadItems()
        observeRealtimeEvents()
        observePendingSyncCount()
        observeReconnection()
    }

    fun processIntent(intent: ListDetailIntent) {
        when (intent) {
            is ListDetailIntent.Refresh -> loadItems()
            is ListDetailIntent.ToggleItem -> toggleItem(intent.itemId, intent.isCompleted)
            is ListDetailIntent.OpenItem -> scope.launch {
                if (isTaskList) {
                    _sideEffects.emit(ListDetailSideEffect.NavigateToEditTask(householdId, listId, intent.itemId))
                } else {
                    _sideEffects.emit(ListDetailSideEffect.NavigateToEditItem(householdId, listId, intent.itemId))
                }
            }
            is ListDetailIntent.AddItem -> scope.launch {
                if (isTaskList) {
                    _sideEffects.emit(ListDetailSideEffect.NavigateToCreateTask(householdId, listId))
                } else {
                    _sideEffects.emit(ListDetailSideEffect.NavigateToCreateItem(householdId, listId))
                }
            }
            is ListDetailIntent.ToggleCompletedVisibility -> _state.update {
                it.copy(showCompleted = !it.showCompleted)
            }
            is ListDetailIntent.NavigateBack -> scope.launch {
                _sideEffects.emit(ListDetailSideEffect.NavigateBack)
            }
            is ListDetailIntent.DeleteItem -> deleteItem(intent.itemId)
            is ListDetailIntent.UndoOperation -> undoOperation(intent.operationId)
            is ListDetailIntent.RedoOperation -> redoOperation(intent.operationId)
            is ListDetailIntent.CommitPendingDelete -> commitPendingDelete(intent.operationId)
            is ListDetailIntent.UpdateSearchQuery -> _state.update { it.copy(searchQuery = intent.query) }
            is ListDetailIntent.ToggleSearch -> _state.update {
                if (it.isSearching) it.copy(isSearching = false, searchQuery = "") else it.copy(isSearching = true)
            }
            is ListDetailIntent.SetFilter -> {
                _state.update { it.copy(activeFilter = intent.filter) }
                loadItems()
            }
            is ListDetailIntent.ToggleShoppingMode -> {
                var shouldReloadItems = false
                _state.update {
                    val transition = it.toggleShoppingMode()
                    shouldReloadItems = transition.shouldReloadItems
                    transition.state
                }
                if (shouldReloadItems) {
                    loadItems()
                }
            }
            is ListDetailIntent.StartRecording -> startRecording()
            is ListDetailIntent.StopRecording -> stopRecording()
            is ListDetailIntent.VoicePermissionDenied -> scope.launch {
                _sideEffects.emit(resourceError(Res.string.detail_voice_permission_required))
            }
            is ListDetailIntent.DismissVoiceSheet -> _state.update {
                it.copy(showVoiceSheet = false, parsedVoiceItems = emptyList(), voiceTranscription = "")
            }
            is ListDetailIntent.ToggleVoiceItem -> _state.update { state ->
                state.copy(parsedVoiceItems = state.parsedVoiceItems.toggleSelectionAt(intent.index))
            }
            is ListDetailIntent.ConfirmVoiceItems -> confirmVoiceItems()
        }
    }

    fun loadItems() {
        _state.update { it.copy(isLoading = true, error = null, completionExitEntryIds = emptySet()) }
        scope.launch {
            val filter = _state.value.activeFilter
            val status = when (filter) {
                "Pending" -> "Pending"
                "Completed" -> "Completed"
                else -> null
            }
            val createdBy: String? = null

            val result = if (isTaskList) {
                taskRepository.getByList(listId, status, createdBy).map { tasks ->
                    tasks.map { it.toListEntryUi() }
                }
            } else {
                itemRepository.getByList(listId, status, createdBy).map { items ->
                    items.map { it.toListEntryUi() }
                }
            }

            result.fold(
                onSuccess = { entries ->
                    _state.update {
                        it.copy(
                            pendingEntries = entries.filter { entry -> !entry.isCompleted },
                            completedEntries = entries.filter { entry -> entry.isCompleted },
                            isLoading = false,
                        )
                    }
                },
                onFailure = { error ->
                    val fallback = if (isTaskList) Res.string.detail_task_load_failed else Res.string.detail_load_failed
                    _state.update { it.copy(isLoading = false, error = UiText.fromError(error.message, fallback)) }
                },
            )
        }
    }

    private fun toggleItem(itemId: String, isCurrentlyCompleted: Boolean) {
        val entry = findEntry(itemId) ?: return
        val operation = EntryOperation.Completion(
            id = nextOperationId++,
            entry = entry,
            fromCompleted = isCurrentlyCompleted,
            toCompleted = !isCurrentlyCompleted,
        )
        operations[operation.id] = operation

        applyCompletionState(itemId, !isCurrentlyCompleted, animateCompletion = !isCurrentlyCompleted)
        scope.launch {
            setRemoteCompletion(itemId, !isCurrentlyCompleted).onFailure { error ->
                operations.remove(operation.id)
                _state.update { it.copy(completionExitEntryIds = it.completionExitEntryIds - itemId) }
                applyCompletionState(itemId, isCurrentlyCompleted, animateCompletion = false)
                _sideEffects.emit(repositoryError(error.message, updateFailureResource()))
                return@launch
            }

            if (!isCurrentlyCompleted) {
                delay(COMPLETION_EXIT_DELAY_MS)
                moveCompletionExitToCompleted(itemId)
            }
        }

        scope.launch {
            _sideEffects.emit(
                ListDetailSideEffect.ShowUndo(
                    operationId = operation.id,
                    message = completionMessage(!isCurrentlyCompleted),
                    isPendingDelete = false,
                ),
            )
        }
    }

    private fun deleteItem(itemId: String) {
        val entry = findEntry(itemId) ?: return
        val operation = EntryOperation.PendingDelete(
            id = nextOperationId++,
            entry = entry,
            wasCompleted = entry.isCompleted,
        )
        operations[operation.id] = operation
        removeEntry(itemId)

        scope.launch {
            _sideEffects.emit(
                ListDetailSideEffect.ShowUndo(
                    operationId = operation.id,
                    message = deleteMessage(),
                    isPendingDelete = true,
                ),
            )
        }
    }

    private fun undoOperation(operationId: Long) {
        val operation = operations.remove(operationId) ?: return
        when (operation) {
            is EntryOperation.Completion -> {
                applyCompletionState(operation.entry.id, operation.fromCompleted, animateCompletion = false)
                redoOperations[operation.id] = operation
                scope.launch {
                    setRemoteCompletion(operation.entry.id, operation.fromCompleted).onFailure { error ->
                        _sideEffects.emit(repositoryError(error.message, updateFailureResource()))
                    }
                    _sideEffects.emit(ListDetailSideEffect.ShowRedo(operation.id, completionMessage(operation.toCompleted)))
                }
            }
            is EntryOperation.PendingDelete -> {
                restoreEntry(operation.entry, operation.wasCompleted)
                redoOperations[operation.id] = operation
                scope.launch {
                    _sideEffects.emit(ListDetailSideEffect.ShowRedo(operation.id, deleteMessage()))
                }
            }
        }
    }

    private fun redoOperation(operationId: Long) {
        val operation = redoOperations.remove(operationId) ?: return
        operations[operation.id] = operation
        when (operation) {
            is EntryOperation.Completion -> {
                applyCompletionState(operation.entry.id, operation.toCompleted, animateCompletion = operation.toCompleted)
                scope.launch {
                    setRemoteCompletion(operation.entry.id, operation.toCompleted).onFailure { error ->
                        _sideEffects.emit(repositoryError(error.message, updateFailureResource()))
                        return@launch
                    }
                    if (operation.toCompleted) {
                        delay(COMPLETION_EXIT_DELAY_MS)
                        moveCompletionExitToCompleted(operation.entry.id)
                    }
                    _sideEffects.emit(
                        ListDetailSideEffect.ShowUndo(operation.id, completionMessage(operation.toCompleted), isPendingDelete = false),
                    )
                }
            }
            is EntryOperation.PendingDelete -> {
                removeEntry(operation.entry.id)
                scope.launch {
                    _sideEffects.emit(ListDetailSideEffect.ShowUndo(operation.id, deleteMessage(), isPendingDelete = true))
                }
            }
        }
    }

    private fun commitPendingDelete(operationId: Long) {
        val operation = operations.remove(operationId) as? EntryOperation.PendingDelete ?: return
        scope.launch {
            deleteRemote(operation.entry.id).onFailure { error ->
                restoreEntry(operation.entry, operation.wasCompleted)
                _sideEffects.emit(repositoryError(error.message, deleteFailureResource()))
            }
        }
    }

    private fun applyCompletionState(itemId: String, completed: Boolean, animateCompletion: Boolean) {
        _state.update { current ->
            val exitIds = if (animateCompletion) {
                current.completionExitEntryIds + itemId
            } else {
                current.completionExitEntryIds - itemId
            }

            if (completed) {
                val entry = current.pendingEntries.find { it.id == itemId }
                if (entry != null) {
                    current.copy(
                        pendingEntries = current.pendingEntries.map {
                            if (it.id == itemId) it.copy(isCompleted = true) else it
                        },
                        completionExitEntryIds = exitIds,
                    )
                } else {
                    current.copy(completionExitEntryIds = exitIds)
                }
            } else {
                val entry = current.completedEntries.find { it.id == itemId }
                if (entry != null) {
                    current.copy(
                        pendingEntries = current.pendingEntries + entry.copy(isCompleted = false, completedByName = null, completedAt = null),
                        completedEntries = current.completedEntries.filter { it.id != itemId },
                        completionExitEntryIds = exitIds,
                    )
                } else {
                    current.copy(
                        pendingEntries = current.pendingEntries.map {
                            if (it.id == itemId) it.copy(isCompleted = false, completedByName = null, completedAt = null) else it
                        },
                        completionExitEntryIds = exitIds,
                    )
                }
            }
        }
    }

    private fun moveCompletionExitToCompleted(itemId: String) {
        _state.update { current ->
            if (itemId !in current.completionExitEntryIds) {
                return@update current
            }
            val entry = current.pendingEntries.find { it.id == itemId } ?: return@update current.copy(
                completionExitEntryIds = current.completionExitEntryIds - itemId,
            )
            current.copy(
                pendingEntries = current.pendingEntries.filter { it.id != itemId },
                completedEntries = listOf(entry.copy(isCompleted = true)) + current.completedEntries.filter { it.id != itemId },
                completionExitEntryIds = current.completionExitEntryIds - itemId,
            )
        }
    }

    private fun removeEntry(itemId: String) {
        _state.update { current ->
            current.copy(
                pendingEntries = current.pendingEntries.filter { it.id != itemId },
                completedEntries = current.completedEntries.filter { it.id != itemId },
                completionExitEntryIds = current.completionExitEntryIds - itemId,
            )
        }
    }

    private fun restoreEntry(entry: ListEntryUi, completed: Boolean) {
        _state.update { current ->
            val restored = entry.copy(isCompleted = completed)
            if (completed) {
                current.copy(completedEntries = listOf(restored) + current.completedEntries.filter { it.id != entry.id })
            } else {
                current.copy(pendingEntries = current.pendingEntries + restored)
            }
        }
    }

    private fun findEntry(itemId: String): ListEntryUi? {
        val current = _state.value
        return current.pendingEntries.find { it.id == itemId } ?: current.completedEntries.find { it.id == itemId }
    }

    private suspend fun setRemoteCompletion(itemId: String, completed: Boolean): Result<Unit> =
        if (isTaskList) {
            if (completed) taskRepository.complete(listId, itemId) else taskRepository.uncomplete(listId, itemId)
        } else {
            if (completed) itemRepository.complete(listId, itemId) else itemRepository.uncomplete(listId, itemId)
        }

    private suspend fun deleteRemote(itemId: String): Result<Unit> =
        if (isTaskList) taskRepository.delete(listId, itemId) else itemRepository.delete(listId, itemId)

    private fun startRecording() {
        if (isTaskList) return
        try {
            audioRecorder.startRecording()
            recordingStartTime = Clock.System.now().toEpochMilliseconds()
            _state.update { it.copy(isRecording = true) }
        } catch (e: Exception) {
            scope.launch {
                _sideEffects.emit(repositoryError(e.message, Res.string.detail_recording_failed))
            }
        }
    }

    private fun stopRecording() {
        if (isTaskList) return

        val elapsed = Clock.System.now().toEpochMilliseconds() - recordingStartTime
        if (elapsed < MIN_RECORDING_DURATION_MS) {
            audioRecorder.stopRecording()
            _state.update { it.copy(isRecording = false) }
            scope.launch { _sideEffects.emit(resourceError(Res.string.detail_recording_too_short)) }
            return
        }

        val audioData = audioRecorder.stopRecording()
        _state.update { it.copy(isRecording = false, isProcessingVoice = true) }

        if (audioData == null || audioData.isEmpty()) {
            _state.update { it.copy(isProcessingVoice = false) }
            scope.launch { _sideEffects.emit(resourceError(Res.string.detail_no_audio)) }
            return
        }

        if (!networkMonitor.isCurrentlyOnline()) {
            _state.update { it.copy(isProcessingVoice = false) }
            scope.launch { _sideEffects.emit(resourceError(Res.string.detail_no_connection)) }
            return
        }

        scope.launch {
            itemRepository.parseVoiceAudio(listId, audioData).fold(
                onSuccess = { result ->
                    if (result.transcription.isBlank()) {
                        _state.update { it.copy(isProcessingVoice = false) }
                        _sideEffects.emit(resourceError(Res.string.detail_speech_not_recognized))
                    } else {
                        _state.update {
                            it.copy(
                                isProcessingVoice = false,
                                voiceTranscription = result.transcription,
                                parsedVoiceItems = result.items.map { p ->
                                    ParsedVoiceItem(p.title, p.quantity, p.unit, p.matchedExistingItem)
                                },
                                showVoiceSheet = true,
                            )
                        }
                    }
                },
                onFailure = { error ->
                    _state.update { it.copy(isProcessingVoice = false) }
                    _sideEffects.emit(repositoryError(error.message, Res.string.detail_voice_process_failed))
                },
            )
        }
    }

    private fun confirmVoiceItems() {
        if (isTaskList) return

        val selected = _state.value.parsedVoiceItems.filter { it.isSelected }
        if (selected.isEmpty()) {
            _state.update { it.copy(showVoiceSheet = false, parsedVoiceItems = emptyList(), voiceTranscription = "") }
            return
        }

        scope.launch {
            val parsedItems = selected.map {
                com.convy.shared.domain.model.ParsedItem(it.title, it.quantity, it.unit, it.matchedExistingItem)
            }
            itemRepository.batchCreate(listId, parsedItems).fold(
                onSuccess = {
                    _state.update { it.copy(showVoiceSheet = false, parsedVoiceItems = emptyList(), voiceTranscription = "") }
                    loadItems()
                },
                onFailure = { error ->
                    _sideEffects.emit(repositoryError(error.message, Res.string.detail_add_items_failed))
                },
            )
        }
    }

    private fun completionMessage(completed: Boolean): UiText {
        val resource = when {
            isTaskList && completed -> Res.string.detail_task_completed
            isTaskList -> Res.string.detail_task_pending
            completed -> Res.string.detail_item_completed
            else -> Res.string.detail_item_pending
        }
        return UiText.StringResourceText(resource)
    }

    private fun deleteMessage(): UiText =
        UiText.StringResourceText(if (isTaskList) Res.string.detail_task_deleted else Res.string.detail_item_deleted)

    private fun updateFailureResource(): StringResource =
        if (isTaskList) Res.string.detail_task_update_failed else Res.string.detail_update_failed

    private fun deleteFailureResource(): StringResource =
        if (isTaskList) Res.string.detail_task_delete_failed else Res.string.detail_delete_failed

    private fun resourceError(resource: StringResource) =
        ListDetailSideEffect.ShowError(UiText.StringResourceText(resource))

    private fun repositoryError(message: String?, fallback: StringResource) =
        ListDetailSideEffect.ShowError(UiText.fromError(message, fallback))

    private fun observeRealtimeEvents() {
        scope.launch {
            realtimeService.events.collect { event ->
                when (event) {
                    is HouseholdEvent.ItemCreated,
                    is HouseholdEvent.ItemUpdated,
                    is HouseholdEvent.ItemCompleted,
                    is HouseholdEvent.ItemUncompleted,
                    is HouseholdEvent.ItemDeleted,
                    is HouseholdEvent.TaskCreated,
                    is HouseholdEvent.TaskUpdated,
                    is HouseholdEvent.TaskCompleted,
                    is HouseholdEvent.TaskUncompleted,
                    is HouseholdEvent.TaskDeleted -> loadItems()
                    else -> {}
                }
            }
        }
    }

    private fun observePendingSyncCount() {
        scope.launch {
            offlineQueue.actions
                .map { it.size }
                .distinctUntilChanged()
                .collect { count ->
                    _state.update { it.copy(pendingSyncCount = count) }
                }
        }
    }

    private fun observeReconnection() {
        scope.launch {
            signalRClient.connectionState
                .collect { connectionState ->
                    if (connectionState == ConnectionState.Connected) {
                        loadItems()
                    }
                }
        }
    }

    private sealed class EntryOperation(open val id: Long) {
        data class Completion(
            override val id: Long,
            val entry: ListEntryUi,
            val fromCompleted: Boolean,
            val toCompleted: Boolean,
        ) : EntryOperation(id)

        data class PendingDelete(
            override val id: Long,
            val entry: ListEntryUi,
            val wasCompleted: Boolean,
        ) : EntryOperation(id)
    }

    companion object {
        private const val MIN_RECORDING_DURATION_MS = 1500L
        private const val COMPLETION_EXIT_DELAY_MS = 450L
    }
}
