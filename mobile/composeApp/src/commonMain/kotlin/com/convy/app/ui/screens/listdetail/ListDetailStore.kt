package com.convy.app.ui.screens.listdetail

import com.convy.app.generated.resources.*
import com.convy.app.util.UiText
import com.convy.shared.data.offline.OfflineActionQueue
import com.convy.shared.data.remote.ConnectionState
import com.convy.shared.data.remote.HouseholdEvent
import com.convy.shared.data.remote.HouseholdRealtimeService
import com.convy.shared.data.remote.SignalRClient
import com.convy.shared.domain.repository.ItemRepository
import com.convy.shared.platform.AudioRecorder
import com.convy.shared.platform.NetworkMonitor
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import kotlinx.datetime.Clock

class ListDetailStore(
    private val householdId: String,
    private val listId: String,
    private val listName: String,
    private val listType: String,
    private val itemRepository: ItemRepository,
    private val realtimeService: HouseholdRealtimeService,
    private val audioRecorder: AudioRecorder,
    private val networkMonitor: NetworkMonitor,
    private val offlineQueue: OfflineActionQueue,
    private val signalRClient: SignalRClient,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private var recordingStartTime: Long = 0L
    private val _state = MutableStateFlow(
        ListDetailState(
            listId = listId,
            householdId = householdId,
            listName = listName,
            listType = listType,
        )
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
                _sideEffects.emit(ListDetailSideEffect.NavigateToEditItem(householdId, listId, intent.itemId))
            }
            is ListDetailIntent.AddItem -> scope.launch {
                _sideEffects.emit(ListDetailSideEffect.NavigateToCreateItem(householdId, listId))
            }
            is ListDetailIntent.ToggleCompletedVisibility -> _state.update {
                it.copy(showCompleted = !it.showCompleted)
            }
            is ListDetailIntent.NavigateBack -> scope.launch {
                _sideEffects.emit(ListDetailSideEffect.NavigateBack)
            }
            is ListDetailIntent.DeleteItem -> deleteItem(intent.itemId)
            is ListDetailIntent.UpdateSearchQuery -> _state.update { it.copy(searchQuery = intent.query) }
            is ListDetailIntent.ToggleSearch -> _state.update {
                if (it.isSearching) it.copy(isSearching = false, searchQuery = "") else it.copy(isSearching = true)
            }
            is ListDetailIntent.SetFilter -> {
                _state.update { it.copy(activeFilter = intent.filter) }
                loadItems()
            }
            is ListDetailIntent.ToggleShoppingMode -> _state.update {
                it.copy(isShoppingMode = !it.isShoppingMode)
            }
            is ListDetailIntent.StartRecording -> startRecording()
            is ListDetailIntent.StopRecording -> stopRecording()
            is ListDetailIntent.DismissVoiceSheet -> _state.update {
                it.copy(showVoiceSheet = false, parsedVoiceItems = emptyList(), voiceTranscription = "")
            }
            is ListDetailIntent.ToggleVoiceItem -> _state.update { state ->
                val items = state.parsedVoiceItems.toMutableList()
                items[intent.index] = items[intent.index].copy(isSelected = !items[intent.index].isSelected)
                state.copy(parsedVoiceItems = items)
            }
            is ListDetailIntent.ConfirmVoiceItems -> confirmVoiceItems()
        }
    }

    fun loadItems() {
        _state.update { it.copy(isLoading = true, error = null) }
        scope.launch {
            val filter = _state.value.activeFilter
            val status = when (filter) {
                "Pending" -> "Pending"
                "Completed" -> "Completed"
                else -> null
            }
            val createdBy: String? = null

            itemRepository.getByList(listId, status, createdBy).fold(
                onSuccess = { items ->
                    val pending = items.filter { !it.isCompleted }
                    val completed = items.filter { it.isCompleted }
                    _state.update {
                        it.copy(
                            pendingItems = pending,
                            completedItems = completed,
                            isLoading = false,
                        )
                    }
                },
                onFailure = { error ->
                    _state.update { it.copy(isLoading = false, error = UiText.fromError(error.message, Res.string.detail_load_failed)) }
                },
            )
        }
    }

    private fun toggleItem(itemId: String, isCurrentlyCompleted: Boolean) {
        // Optimistic update: move item between lists immediately
        val snapshot = _state.value.let { it.pendingItems to it.completedItems }

        _state.update { current ->
            if (isCurrentlyCompleted) {
                // Move from completed to pending
                val item = current.completedItems.find { it.id == itemId } ?: return@update current
                val toggled = item.copy(isCompleted = false, completedBy = null, completedByName = null, completedAt = null)
                current.copy(
                    pendingItems = current.pendingItems + toggled,
                    completedItems = current.completedItems.filter { it.id != itemId },
                )
            } else {
                // Move from pending to completed
                val item = current.pendingItems.find { it.id == itemId } ?: return@update current
                val toggled = item.copy(isCompleted = true)
                current.copy(
                    pendingItems = current.pendingItems.filter { it.id != itemId },
                    completedItems = listOf(toggled) + current.completedItems,
                )
            }
        }

        // Fire API call in background — repository handles queueing on failure
        scope.launch {
            val result = if (isCurrentlyCompleted) {
                itemRepository.uncomplete(listId, itemId)
            } else {
                itemRepository.complete(listId, itemId)
            }

            result.onFailure { error ->
                // Revert on non-network failure (network failures return success via queue)
                _state.update { it.copy(pendingItems = snapshot.first, completedItems = snapshot.second) }
                _sideEffects.emit(ListDetailSideEffect.ShowError(error.message ?: "Failed to update item"))
            }
        }
    }

    private fun deleteItem(itemId: String) {
        // Optimistic: remove from UI immediately
        val snapshot = _state.value.let { it.pendingItems to it.completedItems }

        _state.update { current ->
            current.copy(
                pendingItems = current.pendingItems.filter { it.id != itemId },
                completedItems = current.completedItems.filter { it.id != itemId },
            )
        }

        scope.launch {
            itemRepository.delete(listId, itemId).onFailure { error ->
                _state.update { it.copy(pendingItems = snapshot.first, completedItems = snapshot.second) }
                _sideEffects.emit(ListDetailSideEffect.ShowError(error.message ?: "Failed to delete item"))
            }
        }
    }

    private fun startRecording() {
        try {
            audioRecorder.startRecording()
            recordingStartTime = Clock.System.now().toEpochMilliseconds()
            _state.update { it.copy(isRecording = true) }
        } catch (e: Exception) {
            scope.launch {
                _sideEffects.emit(ListDetailSideEffect.ShowError(e.message ?: "Failed to start recording"))
            }
        }
    }

    private fun stopRecording() {
        val elapsed = Clock.System.now().toEpochMilliseconds() - recordingStartTime
        if (elapsed < MIN_RECORDING_DURATION_MS) {
            audioRecorder.stopRecording()
            _state.update { it.copy(isRecording = false) }
            scope.launch { _sideEffects.emit(ListDetailSideEffect.ShowError("Recording too short. Hold the button longer.")) }
            return
        }

        val audioData = audioRecorder.stopRecording()
        _state.update { it.copy(isRecording = false, isProcessingVoice = true) }

        if (audioData == null || audioData.isEmpty()) {
            _state.update { it.copy(isProcessingVoice = false) }
            scope.launch { _sideEffects.emit(ListDetailSideEffect.ShowError("No audio recorded")) }
            return
        }

        if (!networkMonitor.isCurrentlyOnline()) {
            _state.update { it.copy(isProcessingVoice = false) }
            scope.launch { _sideEffects.emit(ListDetailSideEffect.ShowError("No internet connection. Please check your network and try again.")) }
            return
        }

        scope.launch {
            itemRepository.parseVoiceAudio(listId, audioData).fold(
                onSuccess = { result ->
                    if (result.transcription.isBlank()) {
                        _state.update { it.copy(isProcessingVoice = false) }
                        _sideEffects.emit(ListDetailSideEffect.ShowError("Could not recognize speech. Please try again."))
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
                    _sideEffects.emit(ListDetailSideEffect.ShowError(error.message ?: "Failed to process voice input"))
                },
            )
        }
    }

    private fun confirmVoiceItems() {
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
                    _sideEffects.emit(ListDetailSideEffect.ShowError(error.message ?: "Failed to add items. Please try again."))
                },
            )
        }
    }

    private fun observeRealtimeEvents() {
        scope.launch {
            realtimeService.events.collect { event ->
                when (event) {
                    is HouseholdEvent.ItemCreated,
                    is HouseholdEvent.ItemUpdated,
                    is HouseholdEvent.ItemCompleted,
                    is HouseholdEvent.ItemUncompleted,
                    is HouseholdEvent.ItemDeleted -> loadItems()
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
                        // Refresh data to catch any events missed while disconnected
                        loadItems()
                    }
                }
        }
    }

    companion object {
        private const val MIN_RECORDING_DURATION_MS = 1500L
    }
}
