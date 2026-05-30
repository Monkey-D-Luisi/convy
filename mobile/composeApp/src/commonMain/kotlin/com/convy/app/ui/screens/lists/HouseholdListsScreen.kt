package com.convy.app.ui.screens.lists

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.KeyboardArrowDown
import androidx.compose.material.icons.filled.Mic
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.Alignment
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.TextRange
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.dp
import com.convy.app.ui.components.ConvyBackground
import com.convy.app.ui.components.ConvyMetric
import com.convy.app.ui.components.ConvyPanel
import com.convy.app.ui.components.ConvySectionHeader
import com.convy.app.ui.components.ConvySpacing
import com.convy.app.ui.components.EmptyContent
import com.convy.app.ui.components.ErrorContent
import com.convy.app.ui.components.ListCard
import com.convy.app.ui.components.LoadingContent
import com.convy.app.ui.components.convyTextFieldColors
import com.convy.app.ui.components.convyTopAppBarColors
import com.convy.shared.domain.model.Household
import com.convy.shared.domain.model.ListType
import com.convy.app.generated.resources.*
import org.jetbrains.compose.resources.stringResource

@Composable
fun HouseholdListsScreen(
    store: HouseholdListsStore,
    onNavigateToList: (String, String, String, String) -> Unit,
    onNavigateToMembers: (String) -> Unit,
    onNavigateToActivity: (String) -> Unit,
    onNavigateToHousehold: (String) -> Unit,
    onNavigateToHouseholds: (String) -> Unit,
    onNavigateToSettings: () -> Unit,
) {
    val state by store.state.collectAsState()
    val snackbarHostState = remember { SnackbarHostState() }

    DisposableEffect(store) {
        onDispose { store.close() }
    }

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is HouseholdListsSideEffect.NavigateToList ->
                    onNavigateToList(effect.householdId, effect.listId, effect.listName, effect.listType)
                is HouseholdListsSideEffect.NavigateToMembers ->
                    onNavigateToMembers(effect.householdId)
                is HouseholdListsSideEffect.NavigateToActivity ->
                    onNavigateToActivity(effect.householdId)
                is HouseholdListsSideEffect.NavigateToHousehold ->
                    onNavigateToHousehold(effect.householdId)
                is HouseholdListsSideEffect.NavigateToHouseholds ->
                    onNavigateToHouseholds(effect.activeHouseholdId)
                is HouseholdListsSideEffect.NavigateToSettings ->
                    onNavigateToSettings()
                is HouseholdListsSideEffect.ShowError -> snackbarHostState.showSnackbar(effect.message)
            }
        }
    }

    HouseholdListsContent(
        state = state,
        onIntent = store::processIntent,
        snackbarHostState = snackbarHostState,
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HouseholdListsContent(
    state: HouseholdListsState,
    onIntent: (HouseholdListsIntent) -> Unit,
    snackbarHostState: SnackbarHostState = remember { SnackbarHostState() },
) {
    Scaffold(
        topBar = {
            TopAppBar(
                colors = convyTopAppBarColors(),
                title = {
                    TextButton(
                        onClick = { onIntent(HouseholdListsIntent.ShowHouseholdSwitcher) },
                        modifier = Modifier.testTag("Household selector"),
                    ) {
                        Text(
                            text = state.householdName.ifEmpty { stringResource(Res.string.lists_default_title) },
                            maxLines = 1,
                        )
                        Icon(Icons.Default.KeyboardArrowDown, contentDescription = null)
                    }
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
                shape = MaterialTheme.shapes.extraLarge,
                containerColor = MaterialTheme.colorScheme.primary,
                contentColor = MaterialTheme.colorScheme.onPrimary,
            ) {
                Icon(Icons.Default.Add, contentDescription = stringResource(Res.string.lists_create_list))
            }
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
    ) { padding ->
        ConvyBackground(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize(),
        ) {
            when {
                state.isLoading -> LoadingContent()
                state.error != null -> ErrorContent(
                    message = state.error.asString(),
                    onRetry = { onIntent(HouseholdListsIntent.Refresh) },
                )
                state.lists.isEmpty() -> Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(horizontal = ConvySpacing.ScreenHorizontal)
                        .padding(top = ConvySpacing.ScreenTop),
                ) {
                    HomeOverview(shoppingPending = 0, taskPending = 0)
                    Box(modifier = Modifier.weight(1f)) {
                        EmptyContent(stringResource(Res.string.lists_empty))
                    }
                }
                else -> LazyColumn(
                    contentPadding = PaddingValues(
                        start = ConvySpacing.ScreenHorizontal,
                        end = ConvySpacing.ScreenHorizontal,
                        top = ConvySpacing.ScreenTop,
                        bottom = 104.dp,
                    ),
                    verticalArrangement = Arrangement.spacedBy(14.dp),
                ) {
                    item {
                        HomeOverview(
                            shoppingPending = state.lists
                                .filter { it.type == ListType.Shopping }
                                .sumOf { state.pendingCounts[it.id] ?: 0 },
                            taskPending = state.lists
                                .filter { it.type == ListType.Tasks }
                                .sumOf { state.pendingCounts[it.id] ?: 0 },
                        )
                    }
                    item {
                        ConvySectionHeader(title = stringResource(Res.string.home_your_lists))
                    }
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
                        shape = MaterialTheme.shapes.large,
                        colors = convyTextFieldColors(),
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

        if (state.showHouseholdSwitcher) {
            HouseholdSwitcherSheet(
                households = state.households,
                activeHouseholdId = state.householdId,
                onSelectHousehold = { onIntent(HouseholdListsIntent.SwitchHousehold(it)) },
                onManageHouseholds = { onIntent(HouseholdListsIntent.ManageHouseholds) },
                onDismiss = { onIntent(HouseholdListsIntent.DismissHouseholdSwitcher) },
            )
        }
    }
}

@Composable
private fun HomeOverview(
    shoppingPending: Int,
    taskPending: Int,
) {
    Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        Text(
            text = stringResource(Res.string.home_greeting),
            style = MaterialTheme.typography.titleLarge,
            color = MaterialTheme.colorScheme.onSurface,
        )
        Text(
            text = stringResource(Res.string.home_today_at_home),
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
        ConvyPanel(modifier = Modifier.fillMaxWidth()) {
            Row(
                modifier = Modifier.fillMaxWidth().padding(horizontal = 12.dp, vertical = 4.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                ConvyMetric(
                    icon = Icons.Default.ShoppingCart,
                    value = shoppingPending.toString(),
                    label = stringResource(Res.string.home_groceries),
                    modifier = Modifier.weight(1f),
                )
                VerticalDivider(
                    modifier = Modifier.height(72.dp),
                    color = MaterialTheme.colorScheme.outlineVariant,
                )
                ConvyMetric(
                    icon = Icons.Default.Check,
                    value = taskPending.toString(),
                    label = stringResource(Res.string.home_chores),
                    modifier = Modifier.weight(1f),
                    tint = MaterialTheme.colorScheme.secondary,
                )
                VerticalDivider(
                    modifier = Modifier.height(72.dp),
                    color = MaterialTheme.colorScheme.outlineVariant,
                )
                ConvyMetric(
                    icon = Icons.Default.Mic,
                    value = stringResource(Res.string.home_voice_ready),
                    label = stringResource(Res.string.detail_voice_input),
                    modifier = Modifier.weight(1f),
                )
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun HouseholdSwitcherSheet(
    households: List<Household>,
    activeHouseholdId: String,
    onSelectHousehold: (String) -> Unit,
    onManageHouseholds: () -> Unit,
    onDismiss: () -> Unit,
) {
    ModalBottomSheet(onDismissRequest = onDismiss) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp)
                .padding(bottom = 24.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Text(
                text = stringResource(Res.string.households_switch),
                style = MaterialTheme.typography.titleMedium,
                modifier = Modifier.padding(bottom = 4.dp),
            )
            households.forEach { household ->
                ListItem(
                    headlineContent = { Text(household.name, maxLines = 1) },
                    leadingContent = {
                        Icon(
                            imageVector = if (household.id == activeHouseholdId) Icons.Default.Check else Icons.Default.Home,
                            contentDescription = null,
                        )
                    },
                    modifier = Modifier
                        .fillMaxWidth()
                        .testTag("Switch ${household.name}")
                        .clickable { onSelectHousehold(household.id) },
                )
            }
            TextButton(
                onClick = onManageHouseholds,
                modifier = Modifier.fillMaxWidth().testTag("Manage households"),
            ) {
                Text(stringResource(Res.string.households_manage))
            }
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
                    shape = MaterialTheme.shapes.large,
                    colors = convyTextFieldColors(),
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
