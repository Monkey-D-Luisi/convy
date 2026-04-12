package com.convy.app.ui.screens.lists

import com.convy.app.generated.resources.*
import com.convy.app.util.UiText
import com.convy.shared.data.remote.HouseholdEvent
import com.convy.shared.data.remote.HouseholdRealtimeService
import com.convy.shared.domain.model.ListType
import com.convy.shared.domain.model.HouseholdList
import com.convy.shared.domain.repository.HouseholdRepository
import com.convy.shared.domain.repository.ItemRepository
import com.convy.shared.domain.repository.ListRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class HouseholdListsStore(
    private val householdId: String,
    private val householdRepository: HouseholdRepository,
    private val listRepository: ListRepository,
    private val itemRepository: ItemRepository,
    private val realtimeService: HouseholdRealtimeService,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val _state = MutableStateFlow(HouseholdListsState(householdId = householdId))
    val state: StateFlow<HouseholdListsState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<HouseholdListsSideEffect>()
    val sideEffects: SharedFlow<HouseholdListsSideEffect> = _sideEffects.asSharedFlow()

    init {
        loadData()
        connectRealtime()
        observeRealtimeEvents()
    }

    fun processIntent(intent: HouseholdListsIntent) {
        when (intent) {
            is HouseholdListsIntent.Refresh -> loadData()
            is HouseholdListsIntent.OpenList -> scope.launch {
                _sideEffects.emit(
                    HouseholdListsSideEffect.NavigateToList(householdId, intent.listId, intent.listName, intent.listType)
                )
            }
            is HouseholdListsIntent.ShowCreateDialog -> _state.update {
                it.copy(showCreateDialog = true, newListName = "", newListType = ListType.Shopping)
            }
            is HouseholdListsIntent.DismissCreateDialog -> _state.update {
                it.copy(showCreateDialog = false)
            }
            is HouseholdListsIntent.UpdateNewListName -> _state.update { it.copy(newListName = intent.name) }
            is HouseholdListsIntent.UpdateNewListType -> _state.update { it.copy(newListType = intent.type) }
            is HouseholdListsIntent.CreateList -> createList()
            is HouseholdListsIntent.OpenMembers -> scope.launch {
                _sideEffects.emit(HouseholdListsSideEffect.NavigateToMembers(householdId))
            }
            is HouseholdListsIntent.OpenActivity -> scope.launch {
                _sideEffects.emit(HouseholdListsSideEffect.NavigateToActivity(householdId))
            }
            is HouseholdListsIntent.OpenSettings -> scope.launch {
                _sideEffects.emit(HouseholdListsSideEffect.NavigateToSettings)
            }
            is HouseholdListsIntent.ShowRenameDialog -> _state.update {
                it.copy(showRenameDialog = true, renameListId = intent.listId, renameListName = intent.currentName)
            }
            is HouseholdListsIntent.DismissRenameDialog -> _state.update { it.copy(showRenameDialog = false) }
            is HouseholdListsIntent.UpdateRenameListName -> _state.update { it.copy(renameListName = intent.name) }
            is HouseholdListsIntent.ConfirmRenameList -> renameList()
            is HouseholdListsIntent.ShowArchiveConfirmation -> _state.update {
                it.copy(showArchiveConfirmation = true, archiveListId = intent.listId, archiveListName = intent.listName)
            }
            is HouseholdListsIntent.DismissArchiveConfirmation -> _state.update { it.copy(showArchiveConfirmation = false) }
            is HouseholdListsIntent.ConfirmArchiveList -> archiveList()
        }
    }

    private fun loadData() {
        _state.update { it.copy(isLoading = true, error = null) }
        scope.launch {
            householdRepository.getById(householdId).fold(
                onSuccess = { detail ->
                    _state.update { it.copy(householdName = detail.name) }
                },
                onFailure = {},
            )

            listRepository.getByHousehold(householdId).fold(
                onSuccess = { lists ->
                    _state.update { it.copy(lists = lists, isLoading = false) }
                    loadPendingCounts(lists)
                },
                onFailure = { error ->
                    _state.update { it.copy(isLoading = false, error = UiText.fromError(error.message, Res.string.lists_load_failed)) }
                },
            )
        }
    }

    private fun createList() {
        val current = _state.value
        if (current.newListName.isBlank()) return

        scope.launch {
            listRepository.create(householdId, current.newListName, current.newListType).fold(
                onSuccess = {
                    _state.update { it.copy(showCreateDialog = false) }
                    loadData()
                },
                onFailure = { error ->
                    _sideEffects.emit(HouseholdListsSideEffect.ShowError(error.message ?: "Failed to create list"))
                },
            )
        }
    }

    private fun loadPendingCounts(lists: List<HouseholdList>) {
        scope.launch {
            val counts = mutableMapOf<String, Int>()
            for (list in lists) {
                itemRepository.getByList(list.id).onSuccess { items ->
                    counts[list.id] = items.count { !it.isCompleted }
                }
            }
            _state.update { it.copy(pendingCounts = counts) }
        }
    }

    private fun renameList() {
        val current = _state.value
        if (current.renameListName.isBlank()) return
        scope.launch {
            listRepository.rename(householdId, current.renameListId, current.renameListName).fold(
                onSuccess = {
                    _state.update { it.copy(showRenameDialog = false) }
                    loadData()
                },
                onFailure = { error ->
                    _sideEffects.emit(HouseholdListsSideEffect.ShowError(error.message ?: "Failed to rename list"))
                },
            )
        }
    }

    private fun archiveList() {
        val current = _state.value
        scope.launch {
            listRepository.archive(householdId, current.archiveListId).fold(
                onSuccess = {
                    _state.update { it.copy(showArchiveConfirmation = false) }
                    loadData()
                },
                onFailure = { error ->
                    _sideEffects.emit(HouseholdListsSideEffect.ShowError(error.message ?: "Failed to archive list"))
                },
            )
        }
    }

    private fun connectRealtime() {
        scope.launch {
            realtimeService.connect(householdId)
        }
    }

    private fun observeRealtimeEvents() {
        scope.launch {
            realtimeService.events.collect { event ->
                when (event) {
                    is HouseholdEvent.ListCreated,
                    is HouseholdEvent.ListRenamed,
                    is HouseholdEvent.ListArchived,
                    is HouseholdEvent.MemberJoined -> loadData()
                    else -> {}
                }
            }
        }
    }
}
