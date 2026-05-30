package com.convy.app.ui.screens.item

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.convy.app.ui.components.ItemHistorySheet
import com.convy.app.ui.components.LoadingContent
import com.convy.app.generated.resources.*
import com.convy.app.ui.components.ConvyBackground
import com.convy.app.ui.components.ConvyFormSection
import com.convy.app.ui.components.ConvyPrimaryBottomBar
import com.convy.app.ui.components.ConvyPrimaryButton
import com.convy.app.ui.components.ConvySpacing
import com.convy.app.ui.components.convyTextFieldColors
import com.convy.app.ui.components.convyTopAppBarColors
import org.jetbrains.compose.resources.stringResource

@Composable
fun ItemFormScreen(
    store: ItemFormStore,
    onNavigateBack: () -> Unit,
) {
    val state by store.state.collectAsState()
    val snackbarHostState = remember { SnackbarHostState() }

    DisposableEffect(store) {
        onDispose { store.close() }
    }

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is ItemFormSideEffect.NavigateBack -> onNavigateBack()
                is ItemFormSideEffect.ShowError -> snackbarHostState.showSnackbar(effect.message)
            }
        }
    }

    ItemFormContent(
        state = state,
        onIntent = store::processIntent,
        snackbarHostState = snackbarHostState,
    )

    if (state.showHistory) {
        ItemHistorySheet(
            itemTitle = state.title,
            entries = state.historyEntries,
            isLoading = state.isLoadingHistory,
            onDismiss = { store.processIntent(ItemFormIntent.DismissHistory) },
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ItemFormContent(
    state: ItemFormState,
    onIntent: (ItemFormIntent) -> Unit,
    snackbarHostState: SnackbarHostState = remember { SnackbarHostState() },
) {
    Scaffold(
        topBar = {
            TopAppBar(
                colors = convyTopAppBarColors(),
                title = { Text(if (state.isEditing) stringResource(Res.string.item_edit_title) else stringResource(Res.string.item_new_title)) },
                navigationIcon = {
                    IconButton(
                        onClick = { onIntent(ItemFormIntent.NavigateBack) },
                        modifier = Modifier.testTag("Back"),
                    ) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(Res.string.back))
                    }
                },
                actions = {
                    if (state.isEditing) {
                        IconButton(
                            onClick = { onIntent(ItemFormIntent.Delete) },
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
                ItemFormPrimaryAction(
                    isEditing = state.isEditing,
                    isSaving = state.isSaving,
                    enabled = state.title.isNotBlank() && !state.isSaving,
                    onClick = { onIntent(ItemFormIntent.Save) },
                )
            }
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
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
                verticalArrangement = Arrangement.spacedBy(16.dp),
            ) {
                ConvyFormSection(title = stringResource(Res.string.item_details_section)) {
                    TextField(
                        value = state.title,
                        onValueChange = { onIntent(ItemFormIntent.UpdateTitle(it)) },
                        label = { Text(stringResource(Res.string.item_title_label)) },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                        shape = MaterialTheme.shapes.large,
                        colors = convyTextFieldColors(),
                        keyboardOptions = KeyboardOptions(
                            capitalization = KeyboardCapitalization.Sentences,
                            imeAction = ImeAction.Next,
                        ),
                    )

                    if (state.suggestions.isNotEmpty() && !state.isEditing) {
                        LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            items(state.suggestions) { suggestion ->
                                SuggestionChip(
                                    onClick = { onIntent(ItemFormIntent.SelectSuggestion(suggestion)) },
                                    label = { Text(suggestion) },
                                )
                            }
                        }
                    }

                    if (state.duplicateWarning.isNotEmpty()) {
                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            colors = CardDefaults.cardColors(
                                containerColor = MaterialTheme.colorScheme.errorContainer,
                            ),
                        ) {
                            Column(modifier = Modifier.padding(12.dp)) {
                                Text(
                                    text = stringResource(Res.string.item_duplicates_found),
                                    style = MaterialTheme.typography.labelMedium,
                                    color = MaterialTheme.colorScheme.onErrorContainer,
                                )
                                state.duplicateWarning.forEach { dup ->
                                    Text(
                                        text = "- ${dup.title}${dup.quantity?.let { " ($it${dup.unit?.let { u -> " $u" } ?: ""})" } ?: ""}",
                                        style = MaterialTheme.typography.bodySmall,
                                        color = MaterialTheme.colorScheme.onErrorContainer,
                                    )
                                }
                                TextButton(
                                    onClick = { onIntent(ItemFormIntent.DismissDuplicateWarning) },
                                ) {
                                    Text(stringResource(Res.string.dismiss))
                                }
                            }
                        }
                    }

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(12.dp),
                    ) {
                        TextField(
                            value = state.quantity,
                            onValueChange = { onIntent(ItemFormIntent.UpdateQuantity(it)) },
                            label = { Text(stringResource(Res.string.item_qty_label)) },
                            singleLine = true,
                            modifier = Modifier.weight(1f),
                            shape = MaterialTheme.shapes.large,
                            colors = convyTextFieldColors(),
                            keyboardOptions = KeyboardOptions(
                                keyboardType = KeyboardType.Number,
                                imeAction = ImeAction.Next,
                            ),
                        )
                        TextField(
                            value = state.unit,
                            onValueChange = { onIntent(ItemFormIntent.UpdateUnit(it)) },
                            label = { Text(stringResource(Res.string.item_unit_label)) },
                            placeholder = { Text(stringResource(Res.string.item_unit_placeholder)) },
                            singleLine = true,
                            modifier = Modifier.weight(1f),
                            shape = MaterialTheme.shapes.large,
                            colors = convyTextFieldColors(),
                            keyboardOptions = KeyboardOptions(imeAction = ImeAction.Next),
                        )
                    }

                    TextField(
                        value = state.note,
                        onValueChange = { onIntent(ItemFormIntent.UpdateNote(it)) },
                        label = { Text(stringResource(Res.string.item_note_label)) },
                        placeholder = { Text(stringResource(Res.string.item_note_placeholder)) },
                        modifier = Modifier.fillMaxWidth(),
                        shape = MaterialTheme.shapes.large,
                        colors = convyTextFieldColors(),
                        minLines = 2,
                        maxLines = 4,
                    )
                }

                ConvyFormSection(title = stringResource(Res.string.item_recurrence_section)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically,
                    ) {
                        Text(stringResource(Res.string.item_recurring), style = MaterialTheme.typography.bodyLarge)
                        Switch(
                            checked = state.recurrenceFrequency != null,
                            onCheckedChange = { enabled ->
                                if (enabled) {
                                    onIntent(ItemFormIntent.UpdateRecurrenceFrequency(1))
                                    onIntent(ItemFormIntent.UpdateRecurrenceInterval(1))
                                } else {
                                    onIntent(ItemFormIntent.UpdateRecurrenceFrequency(null))
                                    onIntent(ItemFormIntent.UpdateRecurrenceInterval(null))
                                }
                            },
                        )
                    }

                    if (state.recurrenceFrequency != null) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(8.dp),
                        ) {
                            OutlinedTextField(
                                value = (state.recurrenceInterval ?: 1).toString(),
                                onValueChange = { value ->
                                    val interval = value.toIntOrNull()
                                    if (interval != null && interval > 0) {
                                        onIntent(ItemFormIntent.UpdateRecurrenceInterval(interval))
                                    }
                                },
                                label = { Text(stringResource(Res.string.item_every)) },
                                modifier = Modifier.weight(1f),
                                singleLine = true,
                                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            )

                            val frequencies = listOf(stringResource(Res.string.item_freq_daily), stringResource(Res.string.item_freq_weekly), stringResource(Res.string.item_freq_monthly))
                            SingleChoiceSegmentedButtonRow(modifier = Modifier.weight(2f)) {
                                frequencies.forEachIndexed { index, label ->
                                    SegmentedButton(
                                        selected = state.recurrenceFrequency == index,
                                        onClick = { onIntent(ItemFormIntent.UpdateRecurrenceFrequency(index)) },
                                        shape = SegmentedButtonDefaults.itemShape(index, frequencies.size),
                                    ) { Text(label, style = MaterialTheme.typography.labelSmall) }
                                }
                            }
                        }
                    }
                }

                if (state.error != null) {
                    Text(
                        text = state.error.asString(),
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall,
                    )
                }

                if (state.isEditing) {
                    TextButton(onClick = { onIntent(ItemFormIntent.ShowHistory) }) {
                        Text(stringResource(Res.string.item_view_history))
                    }
                }

                Spacer(modifier = Modifier.height(88.dp))
            }
        }
    }
}

@Composable
private fun ItemFormPrimaryAction(
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
                Text(if (isEditing) stringResource(Res.string.item_save_changes) else stringResource(Res.string.item_add))
            }
        }
    }
}
