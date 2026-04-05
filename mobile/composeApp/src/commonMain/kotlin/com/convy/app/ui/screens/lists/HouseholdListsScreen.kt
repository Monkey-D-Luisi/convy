package com.convy.app.ui.screens.lists

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.convy.app.ui.components.EmptyContent
import com.convy.app.ui.components.ErrorContent
import com.convy.app.ui.components.ListCard
import com.convy.app.ui.components.LoadingContent
import com.convy.shared.domain.model.ListType

@Composable
fun HouseholdListsScreen(
    store: HouseholdListsStore,
    onNavigateToList: (String, String, String) -> Unit,
    onNavigateToMembers: (String) -> Unit,
    onNavigateToActivity: (String) -> Unit,
    onNavigateToSettings: () -> Unit,
) {
    val state by store.state.collectAsState()

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is HouseholdListsSideEffect.NavigateToList ->
                    onNavigateToList(effect.householdId, effect.listId, effect.listName)
                is HouseholdListsSideEffect.NavigateToMembers ->
                    onNavigateToMembers(effect.householdId)
                is HouseholdListsSideEffect.NavigateToActivity ->
                    onNavigateToActivity(effect.householdId)
                is HouseholdListsSideEffect.NavigateToSettings ->
                    onNavigateToSettings()
                is HouseholdListsSideEffect.ShowError -> {}
            }
        }
    }

    HouseholdListsContent(state = state, onIntent = store::processIntent)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HouseholdListsContent(
    state: HouseholdListsState,
    onIntent: (HouseholdListsIntent) -> Unit,
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(state.householdName.ifEmpty { "My Home" })
                },
                actions = {
                    IconButton(onClick = { onIntent(HouseholdListsIntent.OpenActivity) }) {
                        Icon(Icons.Default.Notifications, contentDescription = "Activity")
                    }
                    IconButton(onClick = { onIntent(HouseholdListsIntent.OpenMembers) }) {
                        Icon(Icons.Default.Person, contentDescription = "Members")
                    }
                    IconButton(onClick = { onIntent(HouseholdListsIntent.OpenSettings) }) {
                        Icon(Icons.Default.Settings, contentDescription = "Settings")
                    }
                },
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { onIntent(HouseholdListsIntent.ShowCreateDialog) }) {
                Icon(Icons.Default.Add, contentDescription = "Create list")
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
                    onRetry = { onIntent(HouseholdListsIntent.Refresh) },
                )
                state.lists.isEmpty() -> EmptyContent("No lists yet. Tap + to create one!")
                else -> LazyColumn(
                    contentPadding = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    items(state.lists, key = { it.id }) { list ->
                        ListCard(
                            list = list,
                            pendingCount = 0,
                            onClick = { onIntent(HouseholdListsIntent.OpenList(list.id, list.name)) },
                        )
                    }
                }
            }
        }

        if (state.showCreateDialog) {
            CreateListDialog(
                name = state.newListName,
                type = state.newListType,
                onNameChange = { onIntent(HouseholdListsIntent.UpdateNewListName(it)) },
                onTypeChange = { onIntent(HouseholdListsIntent.UpdateNewListType(it)) },
                onConfirm = { onIntent(HouseholdListsIntent.CreateList) },
                onDismiss = { onIntent(HouseholdListsIntent.DismissCreateDialog) },
            )
        }
    }
}

@Composable
private fun CreateListDialog(
    name: String,
    type: ListType,
    onNameChange: (String) -> Unit,
    onTypeChange: (ListType) -> Unit,
    onConfirm: () -> Unit,
    onDismiss: () -> Unit,
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("New list") },
        text = {
            Column {
                TextField(
                    value = name,
                    onValueChange = onNameChange,
                    label = { Text("List name") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp),
                    colors = TextFieldDefaults.colors(
                        unfocusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                        focusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                    ),
                )
                Spacer(modifier = Modifier.height(16.dp))
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    FilterChip(
                        selected = type == ListType.Shopping,
                        onClick = { onTypeChange(ListType.Shopping) },
                        label = { Text("Shopping") },
                        modifier = Modifier.weight(1f),
                    )
                    FilterChip(
                        selected = type == ListType.Tasks,
                        onClick = { onTypeChange(ListType.Tasks) },
                        label = { Text("Tasks") },
                        modifier = Modifier.weight(1f),
                    )
                }
            }
        },
        confirmButton = {
            TextButton(
                onClick = onConfirm,
                enabled = name.isNotBlank(),
            ) {
                Text("Create")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("Cancel")
            }
        },
    )
}
