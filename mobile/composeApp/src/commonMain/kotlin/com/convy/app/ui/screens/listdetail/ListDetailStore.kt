package com.convy.app.ui.screens.listdetail

import com.convy.shared.data.remote.HouseholdEvent
import com.convy.shared.data.remote.HouseholdRealtimeService
import com.convy.shared.domain.repository.ItemRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class ListDetailStore(
    private val householdId: String,
    private val listId: String,
    private val listName: String,
    private val itemRepository: ItemRepository,
    private val realtimeService: HouseholdRealtimeService,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val _state = MutableStateFlow(
        ListDetailState(
            listId = listId,
            householdId = householdId,
            listName = listName,
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
        }
    }

    fun loadItems() {
        _state.update { it.copy(isLoading = true, error = null) }
        scope.launch {
            itemRepository.getByList(listId).fold(
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
