package com.convy.app.ui.screens.households

import androidx.compose.foundation.layout.Arrangement
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
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.ExitToApp
import androidx.compose.material.icons.automirrored.filled.Login
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.Home
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TextField
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.convy.app.generated.resources.*
import com.convy.app.ui.components.EmptyContent
import com.convy.app.ui.components.ErrorContent
import com.convy.app.ui.components.LoadingContent
import com.convy.shared.domain.model.Household
import org.jetbrains.compose.resources.stringResource

@Composable
fun HouseholdsScreen(
    store: HouseholdsStore,
    onNavigateToLists: (String) -> Unit,
    onNavigateToHouseholdSetup: () -> Unit,
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
                is HouseholdsSideEffect.NavigateBack -> onNavigateBack()
                is HouseholdsSideEffect.NavigateToLists -> onNavigateToLists(effect.householdId)
                is HouseholdsSideEffect.NavigateToHouseholdSetup -> onNavigateToHouseholdSetup()
                is HouseholdsSideEffect.ShowError -> snackbarHostState.showSnackbar(effect.message)
            }
        }
    }

    HouseholdsContent(
        state = state,
        onIntent = store::processIntent,
        snackbarHostState = snackbarHostState,
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HouseholdsContent(
    state: HouseholdsState,
    onIntent: (HouseholdsIntent) -> Unit,
    snackbarHostState: SnackbarHostState = remember { SnackbarHostState() },
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(Res.string.households_title)) },
                navigationIcon = {
                    IconButton(
                        onClick = { onIntent(HouseholdsIntent.NavigateBack) },
                        modifier = Modifier.testTag("Back"),
                    ) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(Res.string.back))
                    }
                },
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
    ) { padding ->
        Column(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize(),
        ) {
            when {
                state.isLoading -> LoadingContent()
                state.error != null -> ErrorContent(
                    message = state.error.asString(),
                    onRetry = { onIntent(HouseholdsIntent.Refresh) },
                )
                state.households.isEmpty() -> {
                    HouseholdActions(
                        onCreate = { onIntent(HouseholdsIntent.ShowCreateDialog) },
                        onJoin = { onIntent(HouseholdsIntent.ShowJoinDialog) },
                    )
                    EmptyContent(
                        message = stringResource(Res.string.households_empty),
                        modifier = Modifier.weight(1f),
                    )
                }
                else -> LazyColumn(
                    contentPadding = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    item {
                        HouseholdActions(
                            onCreate = { onIntent(HouseholdsIntent.ShowCreateDialog) },
                            onJoin = { onIntent(HouseholdsIntent.ShowJoinDialog) },
                        )
                    }
                    item {
                        Text(
                            text = stringResource(Res.string.households_your_households),
                            style = MaterialTheme.typography.titleSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            modifier = Modifier.padding(top = 8.dp, bottom = 4.dp),
                        )
                    }
                    items(state.households, key = { it.id }) { household ->
                        HouseholdRow(
                            household = household,
                            isActive = household.id == state.activeHouseholdId,
                            onSelect = { onIntent(HouseholdsIntent.SelectHousehold(household.id)) },
                            onRename = { onIntent(HouseholdsIntent.ShowRenameDialog(household.id, household.name)) },
                            onLeave = { onIntent(HouseholdsIntent.ShowLeaveConfirmation(household.id, household.name)) },
                        )
                    }
                }
            }

            HouseholdsDialogs(state = state, onIntent = onIntent)
        }
    }
}

@Composable
private fun HouseholdActions(
    onCreate: () -> Unit,
    onJoin: () -> Unit,
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 12.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        Button(
            onClick = onCreate,
            modifier = Modifier.weight(1f).height(48.dp).testTag("Create household"),
        ) {
            Icon(Icons.Default.Add, contentDescription = null)
            Spacer(modifier = Modifier.width(8.dp))
            Text(stringResource(Res.string.households_create))
        }
        OutlinedButton(
            onClick = onJoin,
            modifier = Modifier.weight(1f).height(48.dp).testTag("Join household"),
        ) {
            Icon(Icons.AutoMirrored.Filled.Login, contentDescription = null)
            Spacer(modifier = Modifier.width(8.dp))
            Text(stringResource(Res.string.households_join))
        }
    }
}

@Composable
private fun HouseholdRow(
    household: Household,
    isActive: Boolean,
    onSelect: () -> Unit,
    onRename: () -> Unit,
    onLeave: () -> Unit,
) {
    Card(
        onClick = onSelect,
        modifier = Modifier.fillMaxWidth().testTag("Household ${household.name}"),
        colors = CardDefaults.cardColors(
            containerColor = if (isActive) MaterialTheme.colorScheme.primaryContainer else MaterialTheme.colorScheme.surfaceContainerLow,
        ),
    ) {
        Row(
            modifier = Modifier.fillMaxWidth().padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Icon(
                imageVector = if (isActive) Icons.Default.Check else Icons.Default.Home,
                contentDescription = null,
                modifier = Modifier.size(24.dp),
                tint = if (isActive) MaterialTheme.colorScheme.onPrimaryContainer else MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = household.name,
                    style = MaterialTheme.typography.titleMedium,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                    color = if (isActive) MaterialTheme.colorScheme.onPrimaryContainer else MaterialTheme.colorScheme.onSurface,
                )
                if (isActive) {
                    Text(
                        text = stringResource(Res.string.households_active),
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.onPrimaryContainer,
                    )
                }
            }
            IconButton(onClick = onRename, modifier = Modifier.testTag("Rename ${household.name}")) {
                Icon(Icons.Default.Edit, contentDescription = stringResource(Res.string.rename))
            }
            IconButton(onClick = onLeave, modifier = Modifier.testTag("Leave ${household.name}")) {
                Icon(
                    Icons.AutoMirrored.Filled.ExitToApp,
                    contentDescription = stringResource(Res.string.households_leave),
                    tint = MaterialTheme.colorScheme.error,
                )
            }
        }
    }
}

@Composable
private fun HouseholdsDialogs(
    state: HouseholdsState,
    onIntent: (HouseholdsIntent) -> Unit,
) {
    if (state.showCreateDialog) {
        HouseholdTextDialog(
            title = stringResource(Res.string.households_create),
            label = stringResource(Res.string.setup_household_name),
            value = state.newHouseholdName,
            confirmLabel = stringResource(Res.string.create),
            isSubmitting = state.isSubmitting,
            onValueChange = { onIntent(HouseholdsIntent.UpdateNewHouseholdName(it)) },
            onConfirm = { onIntent(HouseholdsIntent.CreateHousehold) },
            onDismiss = { onIntent(HouseholdsIntent.DismissCreateDialog) },
        )
    }

    if (state.showJoinDialog) {
        HouseholdTextDialog(
            title = stringResource(Res.string.households_join),
            label = stringResource(Res.string.setup_invite_code),
            value = state.inviteCode,
            confirmLabel = stringResource(Res.string.households_join),
            isSubmitting = state.isSubmitting,
            onValueChange = { onIntent(HouseholdsIntent.UpdateInviteCode(it)) },
            onConfirm = { onIntent(HouseholdsIntent.JoinHousehold) },
            onDismiss = { onIntent(HouseholdsIntent.DismissJoinDialog) },
        )
    }

    if (state.showRenameDialog) {
        HouseholdTextDialog(
            title = stringResource(Res.string.settings_rename_household),
            label = stringResource(Res.string.settings_household_name_label),
            value = state.renameText,
            confirmLabel = stringResource(Res.string.rename),
            isSubmitting = state.isSubmitting,
            onValueChange = { onIntent(HouseholdsIntent.UpdateRenameText(it)) },
            onConfirm = { onIntent(HouseholdsIntent.ConfirmRename) },
            onDismiss = { onIntent(HouseholdsIntent.DismissRenameDialog) },
        )
    }

    if (state.showLeaveConfirmation) {
        AlertDialog(
            onDismissRequest = { onIntent(HouseholdsIntent.DismissLeaveConfirmation) },
            title = { Text(stringResource(Res.string.households_leave_title)) },
            text = { Text(stringResource(Res.string.households_leave_message, state.leaveHouseholdName)) },
            confirmButton = {
                TextButton(
                    onClick = { onIntent(HouseholdsIntent.ConfirmLeaveHousehold) },
                    enabled = !state.isSubmitting,
                    colors = ButtonDefaults.textButtonColors(contentColor = MaterialTheme.colorScheme.error),
                ) { Text(stringResource(Res.string.households_leave)) }
            },
            dismissButton = {
                TextButton(onClick = { onIntent(HouseholdsIntent.DismissLeaveConfirmation) }) {
                    Text(stringResource(Res.string.cancel))
                }
            },
        )
    }
}

@Composable
private fun HouseholdTextDialog(
    title: String,
    label: String,
    value: String,
    confirmLabel: String,
    isSubmitting: Boolean,
    onValueChange: (String) -> Unit,
    onConfirm: () -> Unit,
    onDismiss: () -> Unit,
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title) },
        text = {
            TextField(
                value = value,
                onValueChange = onValueChange,
                label = { Text(label) },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(12.dp),
                colors = TextFieldDefaults.colors(
                    unfocusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                    focusedIndicatorColor = androidx.compose.ui.graphics.Color.Transparent,
                ),
            )
        },
        confirmButton = {
            TextButton(
                onClick = onConfirm,
                enabled = value.isNotBlank() && !isSubmitting,
            ) {
                if (isSubmitting) {
                    CircularProgressIndicator(modifier = Modifier.size(18.dp), strokeWidth = 2.dp)
                } else {
                    Text(confirmLabel)
                }
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text(stringResource(Res.string.cancel))
            }
        },
    )
}
