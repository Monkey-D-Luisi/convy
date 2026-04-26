package com.convy.app.ui.screens.task

import com.convy.app.util.UiText

data class TaskFormState(
    val listId: String = "",
    val householdId: String = "",
    val taskId: String? = null,
    val title: String = "",
    val note: String = "",
    val isEditing: Boolean = false,
    val isLoading: Boolean = false,
    val isSaving: Boolean = false,
    val error: UiText? = null,
)

sealed interface TaskFormIntent {
    data class UpdateTitle(val title: String) : TaskFormIntent
    data class UpdateNote(val note: String) : TaskFormIntent
    data object Save : TaskFormIntent
    data object Delete : TaskFormIntent
    data object NavigateBack : TaskFormIntent
}

sealed interface TaskFormSideEffect {
    data object NavigateBack : TaskFormSideEffect
}
