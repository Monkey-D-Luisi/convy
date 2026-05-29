package com.convy.app.ui.screens.task

import com.convy.app.util.UiText
import com.convy.shared.domain.model.TaskPriority

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
    val assignees: List<TaskAssigneeUi> = emptyList(),
    val assignedToUserId: String? = null,
    val assignedToUserName: String? = null,
    val dueDate: String = "",
    val reminderAtUtc: String = "",
    val priority: TaskPriority = TaskPriority.Normal,
)

data class TaskAssigneeUi(
    val userId: String,
    val displayName: String,
)

sealed interface TaskFormIntent {
    data class UpdateTitle(val title: String) : TaskFormIntent
    data class UpdateNote(val note: String) : TaskFormIntent
    data class SelectAssignee(val userId: String?, val displayName: String?) : TaskFormIntent
    data class UpdateDueDate(val dueDate: String) : TaskFormIntent
    data class UpdateReminder(val reminderAtUtc: String) : TaskFormIntent
    data class SelectPriority(val priority: TaskPriority) : TaskFormIntent
    data object Save : TaskFormIntent
    data object Delete : TaskFormIntent
    data object NavigateBack : TaskFormIntent
}

sealed interface TaskFormSideEffect {
    data object NavigateBack : TaskFormSideEffect
}
