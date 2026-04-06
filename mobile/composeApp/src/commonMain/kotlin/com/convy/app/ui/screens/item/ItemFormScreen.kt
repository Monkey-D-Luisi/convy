package com.convy.app.ui.screens.item

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.convy.app.ui.components.ItemHistorySheet
import com.convy.app.ui.components.LoadingContent

@Composable
fun ItemFormScreen(
    store: ItemFormStore,
    onNavigateBack: () -> Unit,
) {
    val state by store.state.collectAsState()

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is ItemFormSideEffect.NavigateBack -> onNavigateBack()
                is ItemFormSideEffect.ShowError -> {}
            }
        }
    }

    ItemFormContent(state = state, onIntent = store::processIntent)

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
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(if (state.isEditing) "Edit item" else "New item") },
                navigationIcon = {
                    IconButton(
                        onClick = { onIntent(ItemFormIntent.NavigateBack) },
                        modifier = Modifier.testTag("Back"),
                    ) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
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
                                contentDescription = "Delete",
                                tint = MaterialTheme.colorScheme.error,
                            )
                        }
                    }
                },
            )
        },
    ) { padding ->
        if (state.isLoading) {
            LoadingContent(modifier = Modifier.padding(padding))
            return@Scaffold
        }

        Column(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(16.dp),
        ) {
            TextField(
                value = state.title,
                onValueChange = { onIntent(ItemFormIntent.UpdateTitle(it)) },
                label = { Text("Title *") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(12.dp),
                colors = TextFieldDefaults.colors(
                    unfocusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                    focusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                ),
                keyboardOptions = KeyboardOptions(imeAction = ImeAction.Next),
            )

            if (state.suggestions.isNotEmpty() && !state.isEditing) {
                Spacer(modifier = Modifier.height(8.dp))
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
                Spacer(modifier = Modifier.height(8.dp))
                Card(
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.errorContainer,
                    ),
                ) {
                    Column(modifier = Modifier.padding(12.dp)) {
                        Text(
                            text = "Possible duplicates found:",
                            style = MaterialTheme.typography.labelMedium,
                            color = MaterialTheme.colorScheme.onErrorContainer,
                        )
                        state.duplicateWarning.forEach { dup ->
                            Text(
                                text = "• ${dup.title}${dup.quantity?.let { " ($it${dup.unit?.let { u -> " $u" } ?: ""})" } ?: ""}",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onErrorContainer,
                            )
                        }
                        Spacer(modifier = Modifier.height(4.dp))
                        TextButton(
                            onClick = { onIntent(ItemFormIntent.DismissDuplicateWarning) },
                        ) {
                            Text("Dismiss")
                        }
                    }
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                TextField(
                    value = state.quantity,
                    onValueChange = { onIntent(ItemFormIntent.UpdateQuantity(it)) },
                    label = { Text("Qty") },
                    singleLine = true,
                    modifier = Modifier.weight(1f),
                    shape = RoundedCornerShape(12.dp),
                    colors = TextFieldDefaults.colors(
                        unfocusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                        focusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                    ),
                    keyboardOptions = KeyboardOptions(
                        keyboardType = KeyboardType.Number,
                        imeAction = ImeAction.Next,
                    ),
                )
                TextField(
                    value = state.unit,
                    onValueChange = { onIntent(ItemFormIntent.UpdateUnit(it)) },
                    label = { Text("Unit") },
                    placeholder = { Text("e.g. kg, pcs") },
                    singleLine = true,
                    modifier = Modifier.weight(1f),
                    shape = RoundedCornerShape(12.dp),
                    colors = TextFieldDefaults.colors(
                        unfocusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                        focusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                    ),
                    keyboardOptions = KeyboardOptions(imeAction = ImeAction.Next),
                )
            }

            Spacer(modifier = Modifier.height(16.dp))

            TextField(
                value = state.note,
                onValueChange = { onIntent(ItemFormIntent.UpdateNote(it)) },
                label = { Text("Note") },
                placeholder = { Text("Optional note...") },
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(12.dp),
                colors = TextFieldDefaults.colors(
                    unfocusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                    focusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                ),
                minLines = 2,
                maxLines = 4,
            )

            // Recurrence section
            Row(
                modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text("Recurring", style = MaterialTheme.typography.bodyLarge)
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
                    modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
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
                        label = { Text("Every") },
                        modifier = Modifier.weight(1f),
                        singleLine = true,
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    )

                    val frequencies = listOf("Daily", "Weekly", "Monthly")
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

            if (state.error != null) {
                Spacer(modifier = Modifier.height(12.dp))
                Text(
                    text = state.error,
                    color = MaterialTheme.colorScheme.error,
                    style = MaterialTheme.typography.bodySmall,
                )
            }

            Spacer(modifier = Modifier.height(24.dp))

            if (state.isEditing) {
                TextButton(onClick = { onIntent(ItemFormIntent.ShowHistory) }) {
                    Text("View history")
                }
                Spacer(modifier = Modifier.height(8.dp))
            }

            Button(
                onClick = { onIntent(ItemFormIntent.Save) },
                modifier = Modifier.fillMaxWidth().height(56.dp),
                shape = RoundedCornerShape(28.dp),
                enabled = state.title.isNotBlank() && !state.isSaving,
            ) {
                if (state.isSaving) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(20.dp),
                        strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary,
                    )
                } else {
                    Text(if (state.isEditing) "Save changes" else "Add item")
                }
            }
        }
    }
}
