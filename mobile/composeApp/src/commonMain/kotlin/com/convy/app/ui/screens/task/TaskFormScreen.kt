package com.convy.app.ui.screens.task

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
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
