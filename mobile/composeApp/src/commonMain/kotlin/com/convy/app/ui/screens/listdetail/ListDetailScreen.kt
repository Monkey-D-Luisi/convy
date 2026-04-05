package com.convy.app.ui.screens.listdetail

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.KeyboardArrowDown
import androidx.compose.material.icons.filled.KeyboardArrowUp
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.convy.app.ui.components.EmptyContent
import com.convy.app.ui.components.ErrorContent
import com.convy.app.ui.components.ItemCard
import com.convy.app.ui.components.LoadingContent

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
                title = { Text(state.listName) },
                navigationIcon = {
                    IconButton(onClick = { onIntent(ListDetailIntent.NavigateBack) }) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { onIntent(ListDetailIntent.AddItem) }) {
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
            when {
                state.isLoading -> LoadingContent()
                state.error != null -> ErrorContent(
                    message = state.error,
                    onRetry = { onIntent(ListDetailIntent.Refresh) },
                )
                state.pendingItems.isEmpty() && state.completedItems.isEmpty() -> EmptyContent(
                    "No items yet. Tap + to add one!",
                )
                else -> LazyColumn(
                    contentPadding = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    if (state.pendingItems.isNotEmpty()) {
                        item {
                            Text(
                                text = "Pending (${state.pendingItems.size})",
                                style = MaterialTheme.typography.titleSmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.padding(bottom = 4.dp),
                            )
                        }
                        items(state.pendingItems, key = { it.id }) { item ->
                            ItemCard(
                                item = item,
                                onToggleComplete = { onIntent(ListDetailIntent.ToggleItem(item.id, item.isCompleted)) },
                                onClick = { onIntent(ListDetailIntent.OpenItem(item.id)) },
                            )
                        }
                    }

                    if (state.completedItems.isNotEmpty()) {
                        item {
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(top = 8.dp),
                                verticalAlignment = Alignment.CenterVertically,
                            ) {
                                Text(
                                    text = "Completed (${state.completedItems.size})",
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
                            items(state.completedItems, key = { it.id }) { item ->
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
