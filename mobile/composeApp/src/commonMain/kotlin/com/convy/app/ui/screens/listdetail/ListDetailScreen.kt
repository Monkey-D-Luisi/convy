package com.convy.app.ui.screens.listdetail

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.fadeOut
import androidx.compose.animation.shrinkVertically
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.KeyboardArrowDown
import androidx.compose.material.icons.filled.KeyboardArrowUp
import androidx.compose.material.icons.filled.Mic
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material.icons.filled.Stop
import androidx.compose.material.icons.filled.Sync
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.LocalContentColor
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarDuration
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.SnackbarResult
import androidx.compose.material3.SwipeToDismissBox
import androidx.compose.material3.SwipeToDismissBoxValue
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.rememberSwipeToDismissBoxState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.convy.app.generated.resources.*
import com.convy.app.ui.components.EmptyContent
import com.convy.app.ui.components.ErrorContent
import com.convy.app.ui.components.LoadingContent
import com.convy.app.ui.components.VoiceInputSheet
import com.convy.app.util.UiText
import com.convy.app.util.rememberRecordAudioPermissionState
import org.jetbrains.compose.resources.stringResource

@Composable
fun ListDetailScreen(
    store: ListDetailStore,
    onNavigateToCreateItem: (String, String) -> Unit,
    onNavigateToEditItem: (String, String, String) -> Unit,
    onNavigateToCreateTask: (String, String) -> Unit,
    onNavigateToEditTask: (String, String, String) -> Unit,
    onNavigateBack: () -> Unit,
) {
    val state by store.state.collectAsState()
    val permissionState = rememberRecordAudioPermissionState()
    val snackbarHostState = remember { SnackbarHostState() }
    var pendingRecord by remember { mutableStateOf(false) }
    var nextSnackbarId by remember { mutableStateOf(0) }
    val pendingSnackbarMessages = remember { mutableStateListOf<PendingSnackbarMessage>() }

    LaunchedEffect(permissionState.isGranted) {
        if (permissionState.isGranted && pendingRecord) {
            pendingRecord = false
            store.processIntent(ListDetailIntent.StartRecording)
        }
    }

    LaunchedEffect(permissionState.deniedRequestCount) {
        if (permissionState.deniedRequestCount > 0 && pendingRecord) {
            pendingRecord = false
            store.processIntent(ListDetailIntent.VoicePermissionDenied)
        }
    }

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is ListDetailSideEffect.NavigateToCreateItem ->
                    onNavigateToCreateItem(effect.householdId, effect.listId)
                is ListDetailSideEffect.NavigateToEditItem ->
                    onNavigateToEditItem(effect.householdId, effect.listId, effect.itemId)
                is ListDetailSideEffect.NavigateToCreateTask ->
                    onNavigateToCreateTask(effect.householdId, effect.listId)
                is ListDetailSideEffect.NavigateToEditTask ->
                    onNavigateToEditTask(effect.householdId, effect.listId, effect.taskId)
                is ListDetailSideEffect.NavigateBack -> onNavigateBack()
                is ListDetailSideEffect.ShowError -> {
                    pendingSnackbarMessages.add(PendingSnackbarMessage(nextSnackbarId, effect.message))
                    nextSnackbarId += 1
                }
                is ListDetailSideEffect.ShowUndo -> {
                    pendingSnackbarMessages.add(
                        PendingSnackbarMessage(
                            id = nextSnackbarId,
                            message = effect.message,
                            operationId = effect.operationId,
                            action = SnackbarAction.Undo(effect.isPendingDelete),
                        ),
                    )
                    nextSnackbarId += 1
                }
                is ListDetailSideEffect.ShowRedo -> {
                    pendingSnackbarMessages.add(
                        PendingSnackbarMessage(
                            id = nextSnackbarId,
                            message = effect.message,
                            operationId = effect.operationId,
                            action = SnackbarAction.Redo,
                        ),
                    )
                    nextSnackbarId += 1
                }
            }
        }
    }

    pendingSnackbarMessages.firstOrNull()?.let { pendingMessage ->
        val resolvedMessage = pendingMessage.message.asString()
        val undoLabel = stringResource(Res.string.detail_undo)
        val redoLabel = stringResource(Res.string.detail_redo)
        val actionLabel = when (pendingMessage.action) {
            is SnackbarAction.Undo -> undoLabel
            SnackbarAction.Redo -> redoLabel
            null -> null
        }
        LaunchedEffect(pendingMessage.id, resolvedMessage, actionLabel) {
            val result = snackbarHostState.showSnackbar(
                message = resolvedMessage,
                actionLabel = actionLabel,
                withDismissAction = actionLabel != null,
                duration = if (actionLabel == null) SnackbarDuration.Short else SnackbarDuration.Long,
            )
            pendingSnackbarMessages.remove(pendingMessage)
            when (val action = pendingMessage.action) {
                is SnackbarAction.Undo -> {
                    if (result == SnackbarResult.ActionPerformed && pendingMessage.operationId != null) {
                        store.processIntent(ListDetailIntent.UndoOperation(pendingMessage.operationId))
                    } else if (action.isPendingDelete && pendingMessage.operationId != null) {
                        store.processIntent(ListDetailIntent.CommitPendingDelete(pendingMessage.operationId))
                    }
                }
                SnackbarAction.Redo -> {
                    if (result == SnackbarResult.ActionPerformed && pendingMessage.operationId != null) {
                        store.processIntent(ListDetailIntent.RedoOperation(pendingMessage.operationId))
                    }
                }
                null -> {}
            }
        }
    }

    if (state.showVoiceSheet) {
        VoiceInputSheet(
            transcription = state.voiceTranscription,
            items = state.parsedVoiceItems,
            onToggleItem = { store.processIntent(ListDetailIntent.ToggleVoiceItem(it)) },
            onConfirm = { store.processIntent(ListDetailIntent.ConfirmVoiceItems) },
            onDismiss = { store.processIntent(ListDetailIntent.DismissVoiceSheet) },
        )
    }

    ListDetailContent(
        state = state,
        onIntent = { intent ->
            when (intent) {
                is ListDetailIntent.StartRecording -> {
                    if (permissionState.isGranted) {
                        store.processIntent(intent)
                    } else {
                        pendingRecord = true
                        permissionState.launchRequest()
                    }
                }
                else -> store.processIntent(intent)
            }
        },
        snackbarHostState = snackbarHostState,
    )
}

private data class PendingSnackbarMessage(
    val id: Int,
    val message: UiText,
    val operationId: Long? = null,
    val action: SnackbarAction? = null,
)

private sealed interface SnackbarAction {
    data class Undo(val isPendingDelete: Boolean) : SnackbarAction
    data object Redo : SnackbarAction
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ListDetailContent(
    state: ListDetailState,
    onIntent: (ListDetailIntent) -> Unit,
    snackbarHostState: SnackbarHostState = remember { SnackbarHostState() },
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    if (state.isSearching) {
                        OutlinedTextField(
                            value = state.searchQuery,
                            onValueChange = { onIntent(ListDetailIntent.UpdateSearchQuery(it)) },
                            placeholder = {
                                Text(
                                    stringResource(
                                        if (state.isTaskList) Res.string.detail_search_tasks_placeholder else Res.string.detail_search_placeholder,
                                    ),
                                )
                            },
                            singleLine = true,
                            modifier = Modifier.fillMaxWidth(),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedBorderColor = Color.Transparent,
                                unfocusedBorderColor = Color.Transparent,
                            ),
                        )
                    } else {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Text(state.listName)
                            if (state.pendingSyncCount > 0) {
                                Spacer(modifier = Modifier.width(8.dp))
                                Icon(
                                    Icons.Default.Sync,
                                    contentDescription = "Syncing ${state.pendingSyncCount} pending changes",
                                    modifier = Modifier.size(18.dp),
                                    tint = MaterialTheme.colorScheme.onSurfaceVariant,
                                )
                            }
                        }
                    }
                },
                navigationIcon = {
                    IconButton(onClick = {
                        if (state.isSearching) {
                            onIntent(ListDetailIntent.ToggleSearch)
                        } else {
                            onIntent(ListDetailIntent.NavigateBack)
                        }
                    }) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(Res.string.back))
                    }
                },
                actions = {
                    if (!state.isSearching) {
                        if (state.showNormalListChrome && !state.isTaskList) {
                            if (state.isRecording) {
                                IconButton(onClick = { onIntent(ListDetailIntent.StopRecording) }) {
                                    Icon(
                                        Icons.Default.Stop,
                                        contentDescription = stringResource(Res.string.detail_stop_recording),
                                        tint = MaterialTheme.colorScheme.error,
                                    )
                                }
                            } else {
                                IconButton(
                                    onClick = { onIntent(ListDetailIntent.StartRecording) },
                                    enabled = !state.isProcessingVoice,
                                ) {
                                    if (state.isProcessingVoice) {
                                        CircularProgressIndicator(
                                            modifier = Modifier.size(24.dp),
                                            strokeWidth = 2.dp,
                                        )
                                    } else {
                                        Icon(Icons.Default.Mic, contentDescription = stringResource(Res.string.detail_voice_input))
                                    }
                                }
                            }
                        }
                        if (!state.isTaskList) {
                            IconButton(onClick = { onIntent(ListDetailIntent.ToggleShoppingMode) }) {
                                Icon(
                                    Icons.Default.ShoppingCart,
                                    contentDescription = stringResource(Res.string.detail_shopping_mode),
                                    tint = if (state.isShoppingMode) MaterialTheme.colorScheme.primary else LocalContentColor.current,
                                )
                            }
                        }
                        if (state.showNormalListChrome) {
                            IconButton(onClick = { onIntent(ListDetailIntent.ToggleSearch) }) {
                                Icon(Icons.Default.Search, contentDescription = stringResource(Res.string.search))
                            }
                        }
                    } else {
                        IconButton(onClick = { onIntent(ListDetailIntent.ToggleSearch) }) {
                            Icon(Icons.Default.Close, contentDescription = stringResource(Res.string.close_search))
                        }
                    }
                },
            )
        },
        floatingActionButton = {
            if (state.showNormalListChrome) {
                val addText = stringResource(if (state.isTaskList) Res.string.detail_add_task else Res.string.detail_add_item)
                FloatingActionButton(
                    onClick = { onIntent(ListDetailIntent.AddItem) },
                    modifier = Modifier.testTag(addText),
                ) {
                    Icon(Icons.Default.Add, contentDescription = addText)
                }
            }
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
    ) { padding ->
        Box(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize()
                .background(MaterialTheme.colorScheme.surfaceContainerLow),
        ) {
            val query = state.searchQuery.lowercase()
            val filteredPending = if (query.isBlank()) state.pendingEntries else state.pendingEntries.filter { it.title.lowercase().contains(query) }
            val filteredCompleted = if (query.isBlank()) state.completedEntries else state.completedEntries.filter { it.title.lowercase().contains(query) }

            Column(modifier = Modifier.fillMaxSize()) {
                val filterAll = stringResource(Res.string.detail_filter_all)
                val filterPending = stringResource(Res.string.detail_filter_pending)
                val filterCompleted = stringResource(Res.string.detail_filter_completed)
                val filters = listOf("All" to filterAll, "Pending" to filterPending, "Completed" to filterCompleted)
                if (state.showNormalListChrome) {
                    LazyRow(
                        contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                    ) {
                        items(filters.size) { index ->
                            val (filter, label) = filters[index]
                            FilterChip(
                                selected = state.activeFilter == filter,
                                onClick = { onIntent(ListDetailIntent.SetFilter(filter)) },
                                label = { Text(label) },
                            )
                        }
                    }
                }

                when {
                    state.isLoading -> LoadingContent()
                    state.error != null -> ErrorContent(
                        message = state.error.asString(),
                        onRetry = { onIntent(ListDetailIntent.Refresh) },
                    )
                    filteredPending.isEmpty() && filteredCompleted.isEmpty() && state.searchQuery.isNotBlank() -> EmptyContent(
                        stringResource(if (state.isTaskList) Res.string.detail_no_task_search_results else Res.string.detail_no_search_results),
                    )
                    state.pendingEntries.isEmpty() && state.completedEntries.isEmpty() -> EmptyContent(
                        stringResource(if (state.isTaskList) Res.string.detail_tasks_empty else Res.string.detail_empty),
                    )
                    state.isShoppingMode -> ShoppingModeList(
                        pendingEntries = state.pendingEntries,
                        completedEntries = state.completedEntries,
                        onIntent = onIntent,
                    )
                    else -> NormalEntryList(
                        pendingEntries = filteredPending,
                        completedEntries = filteredCompleted,
                        completionExitEntryIds = state.completionExitEntryIds,
                        showCompleted = state.showCompleted,
                        onIntent = onIntent,
                    )
                }
            }
        }
    }
}

@Composable
private fun ShoppingModeList(
    pendingEntries: List<ListEntryUi>,
    completedEntries: List<ListEntryUi>,
    onIntent: (ListDetailIntent) -> Unit,
) {
    val allItems = pendingEntries + completedEntries
    val doneCount = completedEntries.size
    val totalCount = allItems.size
    Column(modifier = Modifier.fillMaxSize()) {
        LinearProgressIndicator(
            progress = { if (totalCount > 0) doneCount.toFloat() / totalCount else 0f },
            modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
        )
        Text(
            text = stringResource(Res.string.detail_done_progress, doneCount, totalCount),
            style = MaterialTheme.typography.labelLarge,
            modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp),
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
        LazyColumn(
            contentPadding = PaddingValues(start = 16.dp, top = 16.dp, end = 16.dp, bottom = 96.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            items(pendingEntries, key = { it.id }) { entry ->
                ShoppingModeEntry(
                    entry = entry,
                    onToggleComplete = { onIntent(ListDetailIntent.ToggleItem(entry.id, false)) },
                )
            }
            if (completedEntries.isNotEmpty()) {
                item {
                    HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))
                }
                items(completedEntries, key = { it.id }) { entry ->
                    ShoppingModeEntry(
                        entry = entry,
                        onToggleComplete = { onIntent(ListDetailIntent.ToggleItem(entry.id, true)) },
                    )
                }
            }
        }
    }
}

@Composable
private fun NormalEntryList(
    pendingEntries: List<ListEntryUi>,
    completedEntries: List<ListEntryUi>,
    completionExitEntryIds: Set<String>,
    showCompleted: Boolean,
    onIntent: (ListDetailIntent) -> Unit,
) {
    LazyColumn(
        contentPadding = PaddingValues(start = 16.dp, top = 16.dp, end = 16.dp, bottom = 96.dp),
        verticalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        if (pendingEntries.isNotEmpty()) {
            item {
                Text(
                    text = stringResource(Res.string.detail_pending_header, pendingEntries.size),
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(bottom = 4.dp),
                )
            }
            items(pendingEntries, key = { it.id }) { entry ->
                AnimatedVisibility(
                    visible = entry.id !in completionExitEntryIds,
                    exit = fadeOut() + shrinkVertically(),
                ) {
                    DismissibleEntry(
                        entry = entry,
                        onDelete = { onIntent(ListDetailIntent.DeleteItem(entry.id)) },
                    ) {
                        ListEntryCard(
                            entry = entry,
                            onToggleComplete = { onIntent(ListDetailIntent.ToggleItem(entry.id, entry.isCompleted)) },
                            onClick = { onIntent(ListDetailIntent.OpenItem(entry.id)) },
                        )
                    }
                }
            }
        }

        if (completedEntries.isNotEmpty()) {
            item {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(top = 8.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Text(
                        text = stringResource(Res.string.detail_completed_header, completedEntries.size),
                        style = MaterialTheme.typography.titleSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.weight(1f),
                    )
                    IconButton(onClick = { onIntent(ListDetailIntent.ToggleCompletedVisibility) }) {
                        Icon(
                            imageVector = if (showCompleted) Icons.Default.KeyboardArrowUp else Icons.Default.KeyboardArrowDown,
                            contentDescription = if (showCompleted) stringResource(Res.string.detail_hide) else stringResource(Res.string.detail_show),
                        )
                    }
                }
            }

            if (showCompleted) {
                items(completedEntries, key = { it.id }) { entry ->
                    DismissibleEntry(
                        entry = entry,
                        onDelete = { onIntent(ListDetailIntent.DeleteItem(entry.id)) },
                    ) {
                        ListEntryCard(
                            entry = entry,
                            onToggleComplete = { onIntent(ListDetailIntent.ToggleItem(entry.id, entry.isCompleted)) },
                            onClick = { onIntent(ListDetailIntent.OpenItem(entry.id)) },
                        )
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun DismissibleEntry(
    entry: ListEntryUi,
    onDelete: () -> Unit,
    content: @Composable () -> Unit,
) {
    val dismissState = rememberSwipeToDismissBoxState(
        positionalThreshold = { it * 0.65f },
        confirmValueChange = { value ->
            if (value == SwipeToDismissBoxValue.EndToStart) {
                onDelete()
                true
            } else {
                false
            }
        },
    )
    SwipeToDismissBox(
        state = dismissState,
        backgroundContent = {
            val color by animateColorAsState(
                if (dismissState.targetValue == SwipeToDismissBoxValue.EndToStart) {
                    MaterialTheme.colorScheme.errorContainer
                } else {
                    Color.Transparent
                },
            )
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(color, shape = MaterialTheme.shapes.medium)
                    .padding(horizontal = 20.dp),
                contentAlignment = Alignment.CenterEnd,
            ) {
                Icon(
                    Icons.Default.Delete,
                    contentDescription = stringResource(Res.string.delete),
                    tint = MaterialTheme.colorScheme.onErrorContainer,
                )
            }
        },
        enableDismissFromStartToEnd = false,
        content = { content() },
    )
}

@Composable
private fun ListEntryCard(
    entry: ListEntryUi,
    onToggleComplete: () -> Unit,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
) {
    Card(
        onClick = onClick,
        modifier = modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = if (entry.isCompleted) {
                MaterialTheme.colorScheme.surface
            } else {
                MaterialTheme.colorScheme.surfaceContainerLowest
            },
        ),
    ) {
        Row(
            modifier = Modifier.padding(start = 4.dp, end = 16.dp, top = 12.dp, bottom = 12.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Checkbox(
                checked = entry.isCompleted,
                onCheckedChange = { onToggleComplete() },
                modifier = Modifier.testTag("item-checkbox"),
                colors = CheckboxDefaults.colors(
                    checkedColor = MaterialTheme.colorScheme.primary,
                ),
            )
            Column(modifier = Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        text = entry.title,
                        style = MaterialTheme.typography.bodyLarge.copy(
                            textDecoration = if (entry.isCompleted) TextDecoration.LineThrough else TextDecoration.None,
                        ),
                        color = if (entry.isCompleted) {
                            MaterialTheme.colorScheme.onSurfaceVariant
                        } else {
                            MaterialTheme.colorScheme.onSurface
                        },
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                        modifier = Modifier.weight(1f, fill = false),
                    )
                    if (entry.recurrenceFrequency != null) {
                        Spacer(modifier = Modifier.width(4.dp))
                        Icon(
                            Icons.Default.Refresh,
                            contentDescription = stringResource(Res.string.item_card_recurring),
                            modifier = Modifier.size(16.dp),
                            tint = MaterialTheme.colorScheme.primary,
                        )
                    }
                }
                val details = buildList {
                    if (entry.quantity != null) {
                        add("${entry.quantity}${entry.unit?.let { " $it" } ?: ""}")
                    }
                    if (entry.note != null) {
                        add(entry.note)
                    }
                }
                if (details.isNotEmpty()) {
                    Spacer(modifier = Modifier.height(2.dp))
                    Text(
                        text = details.joinToString(" · "),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                    )
                }
                Spacer(modifier = Modifier.height(2.dp))
                Text(
                    text = if (entry.isCompleted) {
                        entry.completedAt?.let {
                            stringResource(
                                Res.string.item_card_completed_by_at,
                                entry.completedByName ?: stringResource(Res.string.unknown),
                                formatTimestamp(it),
                            )
                        } ?: stringResource(
                            Res.string.item_card_completed_by,
                            entry.completedByName ?: stringResource(Res.string.unknown),
                        )
                    } else {
                        stringResource(Res.string.item_card_added_by, entry.createdByName, formatTimestamp(entry.createdAt))
                    },
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.outline,
                )
            }
        }
    }
}

@Composable
private fun ShoppingModeEntry(
    entry: ListEntryUi,
    onToggleComplete: () -> Unit,
    modifier: Modifier = Modifier,
) {
    Card(
        modifier = modifier
            .fillMaxWidth()
            .clickable { onToggleComplete() },
        colors = CardDefaults.cardColors(
            containerColor = if (entry.isCompleted) {
                MaterialTheme.colorScheme.surfaceContainerHighest
            } else {
                MaterialTheme.colorScheme.surfaceContainerLowest
            },
        ),
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 20.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            if (entry.isCompleted) {
                Icon(
                    Icons.Default.Check,
                    contentDescription = stringResource(Res.string.item_card_completed),
                    tint = MaterialTheme.colorScheme.primary,
                    modifier = Modifier.size(28.dp),
                )
                Spacer(modifier = Modifier.width(16.dp))
            }
            Text(
                text = entry.title,
                style = MaterialTheme.typography.titleMedium.copy(
                    textDecoration = if (entry.isCompleted) TextDecoration.LineThrough else TextDecoration.None,
                ),
                color = if (entry.isCompleted) {
                    MaterialTheme.colorScheme.onSurfaceVariant
                } else {
                    MaterialTheme.colorScheme.onSurface
                },
                modifier = Modifier.weight(1f),
            )
            if (entry.quantity != null) {
                Text(
                    text = "${entry.quantity}${entry.unit?.let { " $it" } ?: ""}",
                    style = MaterialTheme.typography.titleMedium,
                    color = MaterialTheme.colorScheme.primary,
                )
            }
        }
    }
}

private fun formatTimestamp(iso: String): String {
    return iso.take(16).replace("T", " ")
}
