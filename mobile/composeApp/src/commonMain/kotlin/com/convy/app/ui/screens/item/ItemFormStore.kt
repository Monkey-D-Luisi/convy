package com.convy.app.ui.screens.item

import com.convy.app.generated.resources.*
import com.convy.app.util.UiText
import com.convy.shared.domain.repository.ActivityRepository
import com.convy.shared.domain.repository.ItemRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class ItemFormStore(
    private val householdId: String,
    private val listId: String,
    private val itemId: String?,
    private val itemRepository: ItemRepository,
    private val activityRepository: ActivityRepository,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val _state = MutableStateFlow(
        ItemFormState(
            listId = listId,
            householdId = householdId,
            itemId = itemId,
            isEditing = itemId != null,
        )
    )
    val state: StateFlow<ItemFormState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<ItemFormSideEffect>()
    val sideEffects: SharedFlow<ItemFormSideEffect> = _sideEffects.asSharedFlow()

    private var suggestionJob: Job? = null
    private var duplicateCheckJob: Job? = null

    init {
        if (itemId != null) {
            loadItem()
        } else {
            loadSuggestions("")
        }
    }

    fun processIntent(intent: ItemFormIntent) {
        when (intent) {
            is ItemFormIntent.UpdateTitle -> {
                _state.update { it.copy(title = intent.title) }
                debounceDuplicateCheck(intent.title)
                debounceSuggestions(intent.title)
            }
            is ItemFormIntent.UpdateQuantity -> _state.update { it.copy(quantity = intent.quantity) }
            is ItemFormIntent.UpdateUnit -> _state.update { it.copy(unit = intent.unit) }
            is ItemFormIntent.UpdateNote -> _state.update { it.copy(note = intent.note) }
            is ItemFormIntent.SelectSuggestion -> _state.update {
                it.copy(title = intent.title, suggestions = emptyList())
            }
            is ItemFormIntent.Save -> save()
            is ItemFormIntent.Delete -> delete()
            is ItemFormIntent.NavigateBack -> scope.launch {
                _sideEffects.emit(ItemFormSideEffect.NavigateBack)
            }
            is ItemFormIntent.DismissDuplicateWarning -> _state.update {
                it.copy(duplicateWarning = emptyList())
            }
            is ItemFormIntent.UpdateRecurrenceFrequency -> _state.update {
                it.copy(recurrenceFrequency = intent.frequency)
            }
            is ItemFormIntent.UpdateRecurrenceInterval -> _state.update {
                it.copy(recurrenceInterval = intent.interval)
            }
            is ItemFormIntent.ShowHistory -> loadHistory()
            is ItemFormIntent.DismissHistory -> _state.update { it.copy(showHistory = false) }
        }
    }

    private fun loadItem() {
        _state.update { it.copy(isLoading = true) }
        scope.launch {
            itemRepository.getByList(listId).fold(
                onSuccess = { items ->
                    val item = items.find { it.id == itemId }
                    if (item != null) {
                        val recurrenceFreq = item.recurrenceFrequency?.let {
                            when (it) {
                                "Daily" -> 0
                                "Weekly" -> 1
                                "Monthly" -> 2
                                else -> null
                            }
                        }
                        _state.update {
                            it.copy(
                                title = item.title,
                                quantity = item.quantity?.toString() ?: "",
                                unit = item.unit ?: "",
                                note = item.note ?: "",
                                isLoading = false,
                                recurrenceFrequency = recurrenceFreq,
                                recurrenceInterval = item.recurrenceInterval,
                            )
                        }
                    } else {
                        _state.update { it.copy(isLoading = false, error = UiText.StringResourceText(Res.string.item_not_found)) }
                    }
                },
                onFailure = { error ->
                    _state.update { it.copy(isLoading = false, error = UiText.fromError(error.message, Res.string.item_save_failed)) }
                },
            )
        }
    }

    private fun debounceDuplicateCheck(title: String) {
        if (_state.value.isEditing) return
        duplicateCheckJob?.cancel()
        duplicateCheckJob = scope.launch {
            delay(500)
            if (title.length >= 2) {
                itemRepository.checkDuplicate(listId, title).fold(
                    onSuccess = { check ->
                        _state.update { it.copy(duplicateWarning = check.potentialDuplicates) }
                    },
                    onFailure = {},
                )
            } else {
                _state.update { it.copy(duplicateWarning = emptyList()) }
            }
        }
    }

    private fun debounceSuggestions(query: String) {
        if (_state.value.isEditing) return
        suggestionJob?.cancel()
        suggestionJob = scope.launch {
            delay(300)
            loadSuggestions(query)
        }
    }

    private fun loadSuggestions(query: String) {
        scope.launch {
            itemRepository.getSuggestions(householdId, query.ifBlank { null }).fold(
                onSuccess = { suggestions ->
                    _state.update { it.copy(suggestions = suggestions) }
                },
                onFailure = {},
            )
        }
    }

    private fun save() {
        val current = _state.value
        if (current.title.isBlank() || current.isSaving) return

        _state.update { it.copy(isSaving = true, error = null) }
        val quantity = current.quantity.toIntOrNull()
        val unit = current.unit.ifBlank { null }
        val note = current.note.ifBlank { null }
        val recurrenceFrequency = current.recurrenceFrequency
        val recurrenceInterval = current.recurrenceInterval

        scope.launch {
            val result = if (current.isEditing && current.itemId != null) {
                itemRepository.update(listId, current.itemId, current.title, quantity, unit, note, recurrenceFrequency, recurrenceInterval)
            } else {
                itemRepository.create(listId, current.title, quantity, unit, note, recurrenceFrequency, recurrenceInterval).map { }
            }

            result.fold(
                onSuccess = {
                    _state.update { it.copy(isSaving = false) }
                    _sideEffects.emit(ItemFormSideEffect.NavigateBack)
                },
                onFailure = { error ->
                    _state.update { it.copy(isSaving = false, error = UiText.fromError(error.message, Res.string.item_save_failed)) }
                },
            )
        }
    }

    private fun delete() {
        val current = _state.value
        if (current.itemId == null || current.isSaving) return

        _state.update { it.copy(isSaving = true) }
        scope.launch {
            itemRepository.delete(listId, current.itemId).fold(
                onSuccess = {
                    _state.update { it.copy(isSaving = false) }
                    _sideEffects.emit(ItemFormSideEffect.NavigateBack)
                },
                onFailure = { error ->
                    _state.update { it.copy(isSaving = false, error = UiText.fromError(error.message, Res.string.item_delete_failed)) }
                },
            )
        }
    }

    private fun loadHistory() {
        val id = _state.value.itemId ?: return
        _state.update { it.copy(showHistory = true, isLoadingHistory = true) }
        scope.launch {
            activityRepository.getItemHistory(id).fold(
                onSuccess = { entries ->
                    _state.update { it.copy(historyEntries = entries, isLoadingHistory = false) }
                },
                onFailure = {
                    _state.update { it.copy(isLoadingHistory = false) }
                },
            )
        }
    }
}
