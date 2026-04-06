package com.convy.app.ui.screens.listdetail

import com.convy.shared.data.remote.HouseholdEvent
import com.convy.shared.data.remote.HouseholdRealtimeService
import com.convy.shared.domain.repository.ItemRepository
import com.convy.shared.platform.AudioRecorder
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class ListDetailStore(
    private val householdId: String,
    private val listId: String,
    private val listName: String,
    private val listType: String,
    private val itemRepository: ItemRepository,
    private val realtimeService: HouseholdRealtimeService,
    private val audioRecorder: AudioRecorder,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
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
                    _state.update { it.copy(isLoading = false, error = error.message ?: "Failed to load items") }
                },
            )
        }
    }

    private fun toggleItem(itemId: String, isCurrentlyCompleted: Boolean) {
        scope.launch {
            val result = if (isCurrentlyCompleted) {
                itemRepository.uncomplete(listId, itemId)
            } else {
                itemRepository.complete(listId, itemId)
            }

            result.fold(
                onSuccess = { loadItems() },
                onFailure = { error ->
                    _sideEffects.emit(ListDetailSideEffect.ShowError(error.message ?: "Failed to update item"))
                },
            )
        }
    }

    private fun deleteItem(itemId: String) {
        scope.launch {
            itemRepository.delete(listId, itemId).fold(
                onSuccess = { loadItems() },
                onFailure = { error ->
                    _sideEffects.emit(ListDetailSideEffect.ShowError(error.message ?: "Failed to delete item"))
                },
            )
        }
    }

    private fun startRecording() {
        try {
            audioRecorder.startRecording()
            _state.update { it.copy(isRecording = true) }
        } catch (e: Exception) {
            scope.launch {
                _sideEffects.emit(ListDetailSideEffect.ShowError(e.message ?: "Failed to start recording"))
            }
        }
    }

    private fun stopRecording() {
        val audioData = audioRecorder.stopRecording()
        _state.update { it.copy(isRecording = false, isProcessingVoice = true) }

        if (audioData == null || audioData.isEmpty()) {
            _state.update { it.copy(isProcessingVoice = false) }
            scope.launch { _sideEffects.emit(ListDetailSideEffect.ShowError("No audio recorded")) }
            return
        }

        scope.launch {
            itemRepository.parseVoiceAudio(listId, audioData).fold(
                onSuccess = { result ->
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
        scope.launch {
            selected.forEach { item ->
                itemRepository.create(listId, item.title, item.quantity, item.unit, null)
            }
            _state.update { it.copy(showVoiceSheet = false, parsedVoiceItems = emptyList(), voiceTranscription = "") }
            loadItems()
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
}
