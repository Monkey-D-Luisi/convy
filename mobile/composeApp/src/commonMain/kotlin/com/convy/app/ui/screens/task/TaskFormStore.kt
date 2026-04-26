package com.convy.app.ui.screens.task

import com.convy.app.generated.resources.*
import com.convy.app.util.UiText
import com.convy.shared.domain.repository.TaskRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

class TaskFormStore(
    private val householdId: String,
    private val listId: String,
    private val taskId: String?,
    private val taskRepository: TaskRepository,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val _state = MutableStateFlow(
        TaskFormState(
            listId = listId,
            householdId = householdId,
            taskId = taskId,
            isEditing = taskId != null,
        ),
    )
    val state: StateFlow<TaskFormState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<TaskFormSideEffect>()
    val sideEffects: SharedFlow<TaskFormSideEffect> = _sideEffects.asSharedFlow()

    init {
        if (taskId != null) {
            loadTask()
        }
    }

    fun processIntent(intent: TaskFormIntent) {
        when (intent) {
            is TaskFormIntent.UpdateTitle -> _state.update { it.copy(title = intent.title) }
            is TaskFormIntent.UpdateNote -> _state.update { it.copy(note = intent.note) }
            is TaskFormIntent.Save -> save()
            is TaskFormIntent.Delete -> delete()
            is TaskFormIntent.NavigateBack -> scope.launch {
                _sideEffects.emit(TaskFormSideEffect.NavigateBack)
            }
        }
    }

    private fun loadTask() {
        _state.update { it.copy(isLoading = true, error = null) }
        scope.launch {
            taskRepository.getByList(listId).fold(
                onSuccess = { tasks ->
                    val task = tasks.find { it.id == taskId }
                    if (task != null) {
                        _state.update {
                            it.copy(
                                title = task.title,
                                note = task.note ?: "",
                                isLoading = false,
                            )
                        }
                    } else {
                        _state.update { it.copy(isLoading = false, error = UiText.StringResourceText(Res.string.task_not_found)) }
                    }
                },
                onFailure = { error ->
                    _state.update { it.copy(isLoading = false, error = UiText.fromError(error.message, Res.string.task_save_failed)) }
                },
            )
        }
    }

    private fun save() {
        val current = _state.value
        if (current.title.isBlank() || current.isSaving) return

        _state.update { it.copy(isSaving = true, error = null) }
        val note = current.note.ifBlank { null }

        scope.launch {
            val result = if (current.isEditing && current.taskId != null) {
                taskRepository.update(listId, current.taskId, current.title, note)
            } else {
                taskRepository.create(listId, current.title, note).map { }
            }

            result.fold(
                onSuccess = {
                    _state.update { it.copy(isSaving = false) }
                    _sideEffects.emit(TaskFormSideEffect.NavigateBack)
                },
                onFailure = { error ->
                    _state.update { it.copy(isSaving = false, error = UiText.fromError(error.message, Res.string.task_save_failed)) }
                },
            )
        }
    }

    private fun delete() {
        val current = _state.value
        if (current.taskId == null || current.isSaving) return

        _state.update { it.copy(isSaving = true, error = null) }
        scope.launch {
            taskRepository.delete(listId, current.taskId).fold(
                onSuccess = {
                    _state.update { it.copy(isSaving = false) }
                    _sideEffects.emit(TaskFormSideEffect.NavigateBack)
                },
                onFailure = { error ->
                    _state.update { it.copy(isSaving = false, error = UiText.fromError(error.message, Res.string.task_delete_failed)) }
                },
            )
        }
    }
}
