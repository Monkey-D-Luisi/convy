package com.convy.app.ui.screens.listdetail

import androidx.compose.animation.animateColorAsState
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.KeyboardArrowDown
import androidx.compose.material.icons.filled.KeyboardArrowUp
import androidx.compose.material.icons.filled.Mic
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material.icons.filled.Stop
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.convy.app.ui.components.EmptyContent
import com.convy.app.ui.components.ErrorContent
import com.convy.app.ui.components.ItemCard
import com.convy.app.ui.components.LoadingContent
import com.convy.app.ui.components.ShoppingModeItem
import com.convy.app.ui.components.VoiceInputSheet

@Composable
fun ListDetailScreen(
    store: ListDetailStore,
    onNavigateToCreateItem: (String, String) -> Unit,
    onNavigateToEditItem: (String, String, String) -> Unit,
    onNavigateBack: () -> Unit,
) {
    val state by store.state.collectAsState()

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is ListDetailSideEffect.NavigateToCreateItem ->
                    onNavigateToCreateItem(effect.householdId, effect.listId)
                is ListDetailSideEffect.NavigateToEditItem ->
                    onNavigateToEditItem(effect.householdId, effect.listId, effect.itemId)
                is ListDetailSideEffect.NavigateBack -> onNavigateBack()
                is ListDetailSideEffect.ShowError -> {}
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

    ListDetailContent(state = state, onIntent = store::processIntent)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ListDetailContent(
    state: ListDetailState,
    onIntent: (ListDetailIntent) -> Unit,
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    if (state.isSearching) {
                        OutlinedTextField(
                            value = state.searchQuery,
                            onValueChange = { onIntent(ListDetailIntent.UpdateSearchQuery(it)) },
                            placeholder = { Text("Search items...") },
                            singleLine = true,
                            modifier = Modifier.fillMaxWidth(),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedBorderColor = Color.Transparent,
                                unfocusedBorderColor = Color.Transparent,
                            ),
                        )
                    } else {
                        Text(state.listName)
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
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
                actions = {
                    if (!state.isSearching) {
                        IconButton(onClick = { onIntent(ListDetailIntent.StartVoiceInput) }) {
                            Icon(
                                if (state.isVoiceListening) Icons.Default.Stop else Icons.Default.Mic,
                                contentDescription = "Voice input",
                            )
                        }
                        if (state.listType.equals("Shopping", ignoreCase = true)) {
                            IconButton(onClick = { onIntent(ListDetailIntent.ToggleShoppingMode) }) {
                                Icon(
                                    Icons.Default.ShoppingCart,
                                    contentDescription = "Shopping mode",
                                    tint = if (state.isShoppingMode) MaterialTheme.colorScheme.primary else LocalContentColor.current,
                                )
                            }
                        }
                        IconButton(onClick = { onIntent(ListDetailIntent.ToggleSearch) }) {
                            Icon(Icons.Default.Search, contentDescription = "Search")
                        }
                    } else {
                        IconButton(onClick = { onIntent(ListDetailIntent.ToggleSearch) }) {
                            Icon(Icons.Default.Close, contentDescription = "Close search")
                        }
                    }
                },
            )
        },
        floatingActionButton = {
            FloatingActionButton(
                onClick = { onIntent(ListDetailIntent.AddItem) },
                modifier = Modifier.testTag("Add item"),
            ) {
                Icon(Icons.Default.Add, contentDescription = "Add item")
            }
        },
    ) { padding ->
        Box(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize()
                .background(MaterialTheme.colorScheme.surfaceContainerLow),
        ) {
            val query = state.searchQuery.lowercase()
            val filteredPending = if (query.isBlank()) state.pendingItems else state.pendingItems.filter { it.title.lowercase().contains(query) }
            val filteredCompleted = if (query.isBlank()) state.completedItems else state.completedItems.filter { it.title.lowercase().contains(query) }

            Column(modifier = Modifier.fillMaxSize()) {
                LazyRow(
                    contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    val filters = listOf("All", "Pending", "Completed")
                    items(filters.size) { index ->
                        val filter = filters[index]
                        FilterChip(
                            selected = state.activeFilter == filter,
                            onClick = { onIntent(ListDetailIntent.SetFilter(filter)) },
                            label = { Text(filter) },
                        )
                    }
                }

                when {
                    state.isLoading -> LoadingContent()
                    state.error != null -> ErrorContent(
                        message = state.error,
                        onRetry = { onIntent(ListDetailIntent.Refresh) },
                    )
                    filteredPending.isEmpty() && filteredCompleted.isEmpty() && state.searchQuery.isNotBlank() -> EmptyContent(
                        "No items match your search.",
                    )
                    state.pendingItems.isEmpty() && state.completedItems.isEmpty() -> EmptyContent(
                        "No items yet. Tap + to add one!",
                    )
                    state.isShoppingMode -> {
                        val allItems = state.pendingItems + state.completedItems
                        val doneCount = state.completedItems.size
                        val totalCount = allItems.size
                        Column(modifier = Modifier.fillMaxSize()) {
                            LinearProgressIndicator(
                                progress = { if (totalCount > 0) doneCount.toFloat() / totalCount else 0f },
                                modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                            )
                            Text(
                                text = "$doneCount of $totalCount done",
                                style = MaterialTheme.typography.labelLarge,
                                modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp),
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                            )
                            LazyColumn(
                                contentPadding = PaddingValues(16.dp),
                                verticalArrangement = Arrangement.spacedBy(8.dp),
                            ) {
                                items(state.pendingItems, key = { it.id }) { item ->
                                    ShoppingModeItem(
                                        item = item,
                                        onToggleComplete = { onIntent(ListDetailIntent.ToggleItem(item.id, false)) },
                                    )
                                }
                                if (state.completedItems.isNotEmpty()) {
                                    item {
                                        HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))
                                    }
                                    items(state.completedItems, key = { it.id }) { item ->
                                        ShoppingModeItem(
                                            item = item,
                                            onToggleComplete = { onIntent(ListDetailIntent.ToggleItem(item.id, true)) },
                                        )
                                    }
                                }
                            }
                        }
                    }
                    else -> LazyColumn(
                    contentPadding = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    if (filteredPending.isNotEmpty()) {
                        item {
                            Text(
                                text = "Pending (${filteredPending.size})",
                                style = MaterialTheme.typography.titleSmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.padding(bottom = 4.dp),
                            )
                        }
                        items(filteredPending, key = { it.id }) { item ->
                            val dismissState = rememberSwipeToDismissBoxState(
                                confirmValueChange = { value ->
                                    if (value == SwipeToDismissBoxValue.EndToStart) {
                                        onIntent(ListDetailIntent.DeleteItem(item.id))
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
                                            contentDescription = "Delete",
                                            tint = MaterialTheme.colorScheme.onErrorContainer,
                                        )
                                    }
                                },
                                enableDismissFromStartToEnd = false,
                            ) {
                                ItemCard(
                                    item = item,
                                    onToggleComplete = { onIntent(ListDetailIntent.ToggleItem(item.id, item.isCompleted)) },
                                    onClick = { onIntent(ListDetailIntent.OpenItem(item.id)) },
                                )
                            }
                        }
                    }

                    if (filteredCompleted.isNotEmpty()) {
                        item {
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(top = 8.dp),
                                verticalAlignment = Alignment.CenterVertically,
                            ) {
                                Text(
                                    text = "Completed (${filteredCompleted.size})",
                                    style = MaterialTheme.typography.titleSmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                    modifier = Modifier.weight(1f),
                                )
                                IconButton(
                                    onClick = { onIntent(ListDetailIntent.ToggleCompletedVisibility) },
                                ) {
                                    Icon(
                                        imageVector = if (state.showCompleted) {
                                            Icons.Default.KeyboardArrowUp
                                        } else {
                                            Icons.Default.KeyboardArrowDown
                                        },
                                        contentDescription = if (state.showCompleted) "Hide" else "Show",
                                    )
                                }
                            }
                        }

                        if (state.showCompleted) {
                            items(filteredCompleted, key = { it.id }) { item ->
                                val dismissState = rememberSwipeToDismissBoxState(
                                    confirmValueChange = { value ->
                                        if (value == SwipeToDismissBoxValue.EndToStart) {
                                            onIntent(ListDetailIntent.DeleteItem(item.id))
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
                                                contentDescription = "Delete",
                                                tint = MaterialTheme.colorScheme.onErrorContainer,
                                            )
                                        }
                                    },
                                    enableDismissFromStartToEnd = false,
                                ) {
                                    ItemCard(
                                        item = item,
                                        onToggleComplete = {
                                            onIntent(ListDetailIntent.ToggleItem(item.id, item.isCompleted))
                                        },
                                        onClick = { onIntent(ListDetailIntent.OpenItem(item.id)) },
                                    )
                                }
                            }
                        }
                    }
                }
                }
            }
        }
    }
}
