package com.convy.app.ui.screens.task

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TextField
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.unit.dp
import com.convy.app.generated.resources.*
import com.convy.app.ui.components.ConvyBackground
import com.convy.app.ui.components.ConvyPrimaryBottomBar
import com.convy.app.ui.components.ConvyPrimaryButton
import com.convy.app.ui.components.ConvySpacing
import com.convy.app.ui.components.LoadingContent
import com.convy.app.ui.components.convyTextFieldColors
import com.convy.app.ui.components.convyTopAppBarColors
import com.convy.app.util.formatTaskDate
import com.convy.app.util.formatTaskDateTime
import com.convy.shared.domain.model.TaskPriority
import org.jetbrains.compose.resources.stringResource

@Composable
fun TaskFormScreen(
    store: TaskFormStore,
    onNavigateBack: () -> Unit,
) {
    val state by store.state.collectAsState()

    DisposableEffect(store) {
        onDispose { store.close() }
    }

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is TaskFormSideEffect.NavigateBack -> onNavigateBack()
            }
        }
    }

    TaskFormContent(state = state, onIntent = store::processIntent)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TaskFormContent(
    state: TaskFormState,
    onIntent: (TaskFormIntent) -> Unit,
) {
    Scaffold(
        topBar = {
            TopAppBar(
                colors = convyTopAppBarColors(),
                title = { Text(if (state.isEditing) stringResource(Res.string.task_edit_title) else stringResource(Res.string.task_new_title)) },
                navigationIcon = {
                    IconButton(
                        onClick = { onIntent(TaskFormIntent.NavigateBack) },
                        modifier = Modifier.testTag("Back"),
                    ) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(Res.string.back))
                    }
                },
                actions = {
                    if (state.isEditing) {
                        IconButton(
                            onClick = { onIntent(TaskFormIntent.Delete) },
                            enabled = !state.isSaving,
                            modifier = Modifier.testTag("Delete"),
                        ) {
                            Icon(
                                Icons.Default.Delete,
                                contentDescription = stringResource(Res.string.delete),
                                tint = MaterialTheme.colorScheme.error,
                            )
                        }
                    }
                },
            )
        },
        bottomBar = {
            if (!state.isLoading) {
                TaskFormPrimaryAction(
                    isEditing = state.isEditing,
                    isSaving = state.isSaving,
                    enabled = state.title.isNotBlank() && !state.isSaving,
                    onClick = { onIntent(TaskFormIntent.Save) },
                )
            }
        },
    ) { padding ->
        if (state.isLoading) {
            LoadingContent(modifier = Modifier.padding(padding))
            return@Scaffold
        }

        ConvyBackground(modifier = Modifier.padding(padding)) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState())
                    .padding(ConvySpacing.ScreenHorizontal),
            ) {
                TextField(
                    value = state.title,
                    onValueChange = { onIntent(TaskFormIntent.UpdateTitle(it)) },
                    label = { Text(stringResource(Res.string.task_title_label)) },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth().testTag("Task title"),
                    shape = MaterialTheme.shapes.large,
                    colors = convyTextFieldColors(),
                    keyboardOptions = KeyboardOptions(
                        capitalization = KeyboardCapitalization.Sentences,
                        imeAction = ImeAction.Next,
                    ),
                )

                Spacer(modifier = Modifier.height(16.dp))

                TextField(
                    value = state.note,
                    onValueChange = { onIntent(TaskFormIntent.UpdateNote(it)) },
                    label = { Text(stringResource(Res.string.task_note_label)) },
                    placeholder = { Text(stringResource(Res.string.task_note_placeholder)) },
                    modifier = Modifier.fillMaxWidth().testTag("Task note"),
                    shape = MaterialTheme.shapes.large,
                    colors = convyTextFieldColors(),
                    minLines = 3,
                    maxLines = 6,
                )

                Spacer(modifier = Modifier.height(16.dp))

                TaskAssigneeSelector(state = state, onIntent = onIntent)

                Spacer(modifier = Modifier.height(16.dp))

                TaskDateControls(state = state, onIntent = onIntent)

                Spacer(modifier = Modifier.height(16.dp))

                TaskPrioritySelector(priority = state.priority, onIntent = onIntent)

                if (state.error != null) {
                    Spacer(modifier = Modifier.height(12.dp))
                    Text(
                        text = state.error.asString(),
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall,
                    )
                }

                Spacer(modifier = Modifier.height(104.dp))
            }
        }
    }
}

@Composable
private fun TaskDateControls(
    state: TaskFormState,
    onIntent: (TaskFormIntent) -> Unit,
) {
    Text(
        text = stringResource(Res.string.task_due_date_label),
        style = MaterialTheme.typography.labelLarge,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
    )
    Spacer(modifier = Modifier.height(8.dp))
    Button(
        onClick = { onIntent(TaskFormIntent.OpenDueDatePicker) },
        modifier = Modifier.fillMaxWidth().testTag("Task due date"),
        shape = MaterialTheme.shapes.large,
    ) {
        Text(formatTaskDate(state.dueDate) ?: stringResource(Res.string.task_due_date_none))
    }

    Spacer(modifier = Modifier.height(16.dp))

    Text(
        text = stringResource(Res.string.task_reminder_label),
        style = MaterialTheme.typography.labelLarge,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
    )
    Spacer(modifier = Modifier.height(8.dp))
    Button(
        onClick = { onIntent(TaskFormIntent.OpenReminderPicker) },
        modifier = Modifier.fillMaxWidth().testTag("Task reminder"),
        shape = MaterialTheme.shapes.large,
    ) {
        Text(formatTaskDateTime(state.reminderLocalDateTime) ?: stringResource(Res.string.task_reminder_none))
    }

    if (state.isDueDatePickerOpen) {
        DueDatePickerDialog(state = state, onIntent = onIntent)
    }
    if (state.isReminderPickerOpen) {
        ReminderPickerDialog(state = state, onIntent = onIntent)
    }
}

@Composable
private fun DueDatePickerDialog(
    state: TaskFormState,
    onIntent: (TaskFormIntent) -> Unit,
) {
    AlertDialog(
        onDismissRequest = { onIntent(TaskFormIntent.CloseDueDatePicker) },
        title = { Text(stringResource(Res.string.task_due_date_label)) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                Text(
                    text = formatTaskDate(state.dueDate) ?: stringResource(Res.string.task_due_date_none),
                    style = MaterialTheme.typography.headlineSmall,
                )
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    TextButton(
                        onClick = { onIntent(TaskFormIntent.ShiftDueDate(-1)) },
                        modifier = Modifier.weight(1f),
                    ) {
                        Text(stringResource(Res.string.task_picker_previous_day))
                    }
                    TextButton(
                        onClick = { onIntent(TaskFormIntent.ShiftDueDate(1)) },
                        modifier = Modifier.weight(1f),
                    ) {
                        Text(stringResource(Res.string.task_picker_next_day))
                    }
                }
            }
        },
        dismissButton = {
            TextButton(onClick = { onIntent(TaskFormIntent.ClearDueDate) }) {
                Text(stringResource(Res.string.task_picker_clear))
            }
        },
        confirmButton = {
            TextButton(onClick = { onIntent(TaskFormIntent.CloseDueDatePicker) }) {
                Text(stringResource(Res.string.task_picker_done))
            }
        },
    )
}

@Composable
private fun ReminderPickerDialog(
    state: TaskFormState,
    onIntent: (TaskFormIntent) -> Unit,
) {
    AlertDialog(
        onDismissRequest = { onIntent(TaskFormIntent.CloseReminderPicker) },
        title = { Text(stringResource(Res.string.task_reminder_label)) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                Text(
                    text = formatTaskDateTime(state.reminderLocalDateTime) ?: stringResource(Res.string.task_reminder_none),
                    style = MaterialTheme.typography.headlineSmall,
                )
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    TextButton(
                        onClick = { onIntent(TaskFormIntent.ShiftReminderDays(-1)) },
                        modifier = Modifier.weight(1f),
                    ) {
                        Text(stringResource(Res.string.task_picker_previous_day))
                    }
                    TextButton(
                        onClick = { onIntent(TaskFormIntent.ShiftReminderDays(1)) },
                        modifier = Modifier.weight(1f),
                    ) {
                        Text(stringResource(Res.string.task_picker_next_day))
                    }
                }
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    TextButton(
                        onClick = { onIntent(TaskFormIntent.ShiftReminderHours(-1)) },
                        modifier = Modifier.weight(1f),
                    ) {
                        Text(stringResource(Res.string.task_picker_previous_hour))
                    }
                    TextButton(
                        onClick = { onIntent(TaskFormIntent.ShiftReminderHours(1)) },
                        modifier = Modifier.weight(1f),
                    ) {
                        Text(stringResource(Res.string.task_picker_next_hour))
                    }
                }
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    TextButton(
                        onClick = { onIntent(TaskFormIntent.ShiftReminderMinutes(-15)) },
                        modifier = Modifier.weight(1f),
                    ) {
                        Text(stringResource(Res.string.task_picker_previous_15_minutes))
                    }
                    TextButton(
                        onClick = { onIntent(TaskFormIntent.ShiftReminderMinutes(15)) },
                        modifier = Modifier.weight(1f),
                    ) {
                        Text(stringResource(Res.string.task_picker_next_15_minutes))
                    }
                }
            }
        },
        dismissButton = {
            TextButton(onClick = { onIntent(TaskFormIntent.ClearReminder) }) {
                Text(stringResource(Res.string.task_picker_clear))
            }
        },
        confirmButton = {
            TextButton(onClick = { onIntent(TaskFormIntent.CloseReminderPicker) }) {
                Text(stringResource(Res.string.task_picker_done))
            }
        },
    )
}

@Composable
private fun TaskAssigneeSelector(
    state: TaskFormState,
    onIntent: (TaskFormIntent) -> Unit,
) {
    Text(
        text = stringResource(Res.string.task_assignee_label),
        style = MaterialTheme.typography.labelLarge,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
    )
    Spacer(modifier = Modifier.height(8.dp))
    LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        item {
            FilterChip(
                selected = state.assignedToUserId == null,
                onClick = { onIntent(TaskFormIntent.SelectAssignee(null, null)) },
                label = { Text(stringResource(Res.string.task_assignee_unassigned)) },
            )
        }
        items(state.assignees) { assignee ->
            FilterChip(
                selected = state.assignedToUserId == assignee.userId,
                onClick = { onIntent(TaskFormIntent.SelectAssignee(assignee.userId, assignee.displayName)) },
                label = { Text(assignee.displayName) },
            )
        }
    }
}

@Composable
private fun TaskPrioritySelector(
    priority: TaskPriority,
    onIntent: (TaskFormIntent) -> Unit,
) {
    Text(
        text = stringResource(Res.string.task_priority_label),
        style = MaterialTheme.typography.labelLarge,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
    )
    Spacer(modifier = Modifier.height(8.dp))
    LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        items(TaskPriority.entries.toList()) { option ->
            FilterChip(
                selected = priority == option,
                onClick = { onIntent(TaskFormIntent.SelectPriority(option)) },
                label = { Text(taskPriorityLabel(option)) },
            )
        }
    }
}

@Composable
private fun taskPriorityLabel(priority: TaskPriority): String =
    when (priority) {
        TaskPriority.Low -> stringResource(Res.string.task_priority_low)
        TaskPriority.Normal -> stringResource(Res.string.task_priority_normal)
        TaskPriority.High -> stringResource(Res.string.task_priority_high)
    }

@Composable
private fun TaskFormPrimaryAction(
    isEditing: Boolean,
    isSaving: Boolean,
    enabled: Boolean,
    onClick: () -> Unit,
) {
    ConvyPrimaryBottomBar {
        ConvyPrimaryButton(
            onClick = onClick,
            modifier = Modifier
                .fillMaxWidth(),
            enabled = enabled,
        ) {
            if (isSaving) {
                CircularProgressIndicator(
                    modifier = Modifier.size(20.dp),
                    strokeWidth = 2.dp,
                    color = MaterialTheme.colorScheme.onPrimary,
                )
            } else {
                Text(if (isEditing) stringResource(Res.string.task_save_changes) else stringResource(Res.string.task_add))
            }
        }
    }
}
