package com.convy.app.ui.screens.lists

import com.convy.shared.domain.model.ListType
import com.convy.shared.domain.repository.HouseholdRepository
import com.convy.shared.domain.repository.ListRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class HouseholdListsStore(
    private val householdId: String,
    private val householdRepository: HouseholdRepository,
    private val listRepository: ListRepository,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val _state = MutableStateFlow(HouseholdListsState(householdId = householdId))
    val state: StateFlow<HouseholdListsState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<HouseholdListsSideEffect>()
    val sideEffects: SharedFlow<HouseholdListsSideEffect> = _sideEffects.asSharedFlow()

    init {
        loadData()
    }

    fun processIntent(intent: HouseholdListsIntent) {
        when (intent) {
            is HouseholdListsIntent.Refresh -> loadData()
            is HouseholdListsIntent.OpenList -> scope.launch {
                _sideEffects.emit(
                    HouseholdListsSideEffect.NavigateToList(householdId, intent.listId, intent.listName)
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
            is HouseholdListsIntent.OpenSettings -> scope.launch {
                _sideEffects.emit(HouseholdListsSideEffect.NavigateToSettings)
            }
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
                },
                onFailure = { error ->
                    _state.update { it.copy(isLoading = false, error = error.message ?: "Failed to load lists") }
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
}
