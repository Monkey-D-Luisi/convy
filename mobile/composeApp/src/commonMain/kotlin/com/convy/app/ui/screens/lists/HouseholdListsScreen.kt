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
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.TextRange
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.dp
import com.convy.app.ui.components.EmptyContent
import com.convy.app.ui.components.ErrorContent
import com.convy.app.ui.components.ListCard
import com.convy.app.ui.components.LoadingContent
import com.convy.shared.domain.model.ListType
import com.convy.app.generated.resources.*
import org.jetbrains.compose.resources.stringResource

@Composable
fun HouseholdListsScreen(
    store: HouseholdListsStore,
    onNavigateToList: (String, String, String, String) -> Unit,
    onNavigateToMembers: (String) -> Unit,
    onNavigateToActivity: (String) -> Unit,
    onNavigateToSettings: () -> Unit,
) {
    val state by store.state.collectAsState()

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is HouseholdListsSideEffect.NavigateToList ->
                    onNavigateToList(effect.householdId, effect.listId, effect.listName, effect.listType)
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
                    Text(state.householdName.ifEmpty { stringResource(Res.string.lists_default_title) })
                },
                actions = {
                    IconButton(
                        onClick = { onIntent(HouseholdListsIntent.OpenActivity) },
                        modifier = Modifier.testTag("Activity"),
                    ) {
                        Icon(Icons.Default.Notifications, contentDescription = stringResource(Res.string.lists_activity))
                    }
                    IconButton(
                        onClick = { onIntent(HouseholdListsIntent.OpenMembers) },
                        modifier = Modifier.testTag("Members"),
                    ) {
                        Icon(Icons.Default.Person, contentDescription = stringResource(Res.string.lists_members))
                    }
                    IconButton(
                        onClick = { onIntent(HouseholdListsIntent.OpenSettings) },
                        modifier = Modifier.testTag("Settings"),
                    ) {
                        Icon(Icons.Default.Settings, contentDescription = stringResource(Res.string.lists_settings))
                    }
                },
            )
        },
        floatingActionButton = {
            FloatingActionButton(
                onClick = { onIntent(HouseholdListsIntent.ShowCreateDialog) },
                modifier = Modifier.testTag("Create list"),
            ) {
                Icon(Icons.Default.Add, contentDescription = stringResource(Res.string.lists_create_list))
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
                    message = state.error.asString(),
                    onRetry = { onIntent(HouseholdListsIntent.Refresh) },
                )
                state.lists.isEmpty() -> EmptyContent(stringResource(Res.string.lists_empty))
                else -> LazyColumn(
                    contentPadding = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    items(state.lists, key = { it.id }) { list ->
                        ListCard(
                            list = list,
                            pendingCount = state.pendingCounts[list.id] ?: 0,
                            onClick = { onIntent(HouseholdListsIntent.OpenList(list.id, list.name, list.type.name)) },
                            onRenameClick = { onIntent(HouseholdListsIntent.ShowRenameDialog(list.id, list.name)) },
                            onArchiveClick = { onIntent(HouseholdListsIntent.ShowArchiveConfirmation(list.id, list.name)) },
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

        if (state.showRenameDialog) {
            AlertDialog(
                onDismissRequest = { onIntent(HouseholdListsIntent.DismissRenameDialog) },
                title = { Text(stringResource(Res.string.lists_rename_title)) },
                text = {
                    val focusRequester = remember { FocusRequester() }
                    val textFieldValue = remember(state.renameListId) {
                        mutableStateOf(
                            TextFieldValue(
                                text = state.renameListName,
                                selection = TextRange(0, state.renameListName.length),
                            ),
                        )
                    }
                    TextField(
                        value = textFieldValue.value,
                        onValueChange = {
                            textFieldValue.value = it
                            onIntent(HouseholdListsIntent.UpdateRenameListName(it.text))
                        },
                        label = { Text(stringResource(Res.string.lists_list_name)) },
                        singleLine = true,
                        modifier = Modifier
                            .fillMaxWidth()
                            .focusRequester(focusRequester)
                            .testTag("rename-list-input"),
                    )
                    LaunchedEffect(Unit) {
                        focusRequester.requestFocus()
                    }
                },
                confirmButton = {
                    TextButton(
                        onClick = { onIntent(HouseholdListsIntent.ConfirmRenameList) },
                        enabled = state.renameListName.isNotBlank(),
                    ) { Text(stringResource(Res.string.rename)) }
                },
                dismissButton = {
                    TextButton(onClick = { onIntent(HouseholdListsIntent.DismissRenameDialog) }) { Text(stringResource(Res.string.cancel)) }
                },
            )
        }

        if (state.showArchiveConfirmation) {
            AlertDialog(
                onDismissRequest = { onIntent(HouseholdListsIntent.DismissArchiveConfirmation) },
                title = { Text(stringResource(Res.string.lists_archive_title)) },
                text = { Text(stringResource(Res.string.lists_archive_message, state.archiveListName)) },
                confirmButton = {
                    TextButton(
                        onClick = { onIntent(HouseholdListsIntent.ConfirmArchiveList) },
                        colors = ButtonDefaults.textButtonColors(contentColor = MaterialTheme.colorScheme.error),
                    ) { Text(stringResource(Res.string.archive)) }
                },
                dismissButton = {
                    TextButton(onClick = { onIntent(HouseholdListsIntent.DismissArchiveConfirmation) }) { Text(stringResource(Res.string.cancel)) }
                },
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
        title = { Text(stringResource(Res.string.lists_new_list)) },
        text = {
            Column {
                TextField(
                    value = name,
                    onValueChange = onNameChange,
                    label = { Text(stringResource(Res.string.lists_list_name)) },
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
                        label = { Text(stringResource(Res.string.lists_type_shopping)) },
                        modifier = Modifier.weight(1f),
                    )
                    FilterChip(
                        selected = type == ListType.Tasks,
                        onClick = { onTypeChange(ListType.Tasks) },
                        label = { Text(stringResource(Res.string.lists_type_tasks)) },
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
                Text(stringResource(Res.string.create))
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text(stringResource(Res.string.cancel))
            }
        },
    )
}
