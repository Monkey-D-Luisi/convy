package com.convy.app.ui.screens.settings

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.ExitToApp
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.TextRange
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.dp
import com.convy.app.generated.resources.*
import org.jetbrains.compose.resources.stringResource

@Composable
fun SettingsScreen(
    store: SettingsStore,
    onNavigateToAuth: () -> Unit,
    onNavigateToHouseholdSetup: () -> Unit,
    onNavigateBack: () -> Unit,
) {
    val state by store.state.collectAsState()

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is SettingsSideEffect.NavigateToAuth -> onNavigateToAuth()
                is SettingsSideEffect.NavigateBack -> onNavigateBack()
                is SettingsSideEffect.NavigateToHouseholdSetup -> onNavigateToHouseholdSetup()
            }
        }
    }

    SettingsContent(state = state, onIntent = store::processIntent)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsContent(
    state: SettingsState,
    onIntent: (SettingsIntent) -> Unit,
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(stringResource(Res.string.settings_title)) },
                navigationIcon = {
                    IconButton(
                        onClick = { onIntent(SettingsIntent.NavigateBack) },
                        modifier = Modifier.testTag("Back"),
                    ) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(Res.string.back))
                    }
                },
            )
        },
    ) { padding ->
        Column(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(16.dp),
        ) {
            // Profile card with avatar
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.surfaceContainerLowest,
                ),
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(24.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                ) {
                    // Large avatar
                    Surface(
                        modifier = Modifier.size(64.dp),
                        shape = MaterialTheme.shapes.extraLarge,
                        color = MaterialTheme.colorScheme.primaryContainer,
                    ) {
                        Box(contentAlignment = Alignment.Center) {
                            Text(
                                text = state.displayName.firstOrNull()?.uppercase() ?: "?",
                                style = MaterialTheme.typography.headlineMedium,
                                color = MaterialTheme.colorScheme.onPrimaryContainer,
                            )
                        }
                    }
                    Spacer(modifier = Modifier.height(16.dp))
                    Text(
                        text = state.displayName.ifEmpty { stringResource(Res.string.settings_unknown_name) },
                        style = MaterialTheme.typography.titleLarge,
                    )
                    Text(
                        text = state.email.ifEmpty { stringResource(Res.string.settings_no_email) },
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }

            if (state.householdName.isNotEmpty()) {
                Spacer(modifier = Modifier.height(16.dp))
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.surfaceContainerLow,
                    ),
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text(stringResource(Res.string.settings_household), style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
                        Spacer(modifier = Modifier.height(4.dp))
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically,
                        ) {
                            Text(state.householdName, style = MaterialTheme.typography.titleMedium)
                            IconButton(
                                onClick = { onIntent(SettingsIntent.ShowRenameDialog) },
                                modifier = Modifier.testTag("Rename household"),
                            ) {
                                Icon(
                                    Icons.Default.Edit,
                                    contentDescription = stringResource(Res.string.rename),
                                    tint = MaterialTheme.colorScheme.onSurfaceVariant,
                                )
                            }
                        }
                    }
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            // App info card
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.surfaceContainerLow,
                ),
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                    ) {
                        Text(stringResource(Res.string.settings_app_version), style = MaterialTheme.typography.bodyMedium)
                        Text(
                            state.appVersion,
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.surfaceContainerLow,
                ),
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Text(
                        stringResource(Res.string.settings_notifications),
                        style = MaterialTheme.typography.titleMedium,
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    NotificationSwitchRow(
                        label = stringResource(Res.string.settings_notify_items_added),
                        checked = state.notificationPreferences.itemsAdded,
                        enabled = !state.isSavingNotificationPreferences,
                        onCheckedChange = {
                            onIntent(SettingsIntent.ToggleNotificationPreference(NotificationPreferenceKey.ItemsAdded, it))
                        },
                    )
                    NotificationSwitchRow(
                        label = stringResource(Res.string.settings_notify_tasks_added),
                        checked = state.notificationPreferences.tasksAdded,
                        enabled = !state.isSavingNotificationPreferences,
                        onCheckedChange = {
                            onIntent(SettingsIntent.ToggleNotificationPreference(NotificationPreferenceKey.TasksAdded, it))
                        },
                    )
                    NotificationSwitchRow(
                        label = stringResource(Res.string.settings_notify_items_completed),
                        checked = state.notificationPreferences.itemsCompleted,
                        enabled = !state.isSavingNotificationPreferences,
                        onCheckedChange = {
                            onIntent(SettingsIntent.ToggleNotificationPreference(NotificationPreferenceKey.ItemsCompleted, it))
                        },
                    )
                    NotificationSwitchRow(
                        label = stringResource(Res.string.settings_notify_tasks_completed),
                        checked = state.notificationPreferences.tasksCompleted,
                        enabled = !state.isSavingNotificationPreferences,
                        onCheckedChange = {
                            onIntent(SettingsIntent.ToggleNotificationPreference(NotificationPreferenceKey.TasksCompleted, it))
                        },
                    )
                    NotificationSwitchRow(
                        label = stringResource(Res.string.settings_notify_item_task_changes),
                        checked = state.notificationPreferences.itemTaskChanges,
                        enabled = !state.isSavingNotificationPreferences,
                        onCheckedChange = {
                            onIntent(SettingsIntent.ToggleNotificationPreference(NotificationPreferenceKey.ItemTaskChanges, it))
                        },
                    )
                    NotificationSwitchRow(
                        label = stringResource(Res.string.settings_notify_list_changes),
                        checked = state.notificationPreferences.listChanges,
                        enabled = !state.isSavingNotificationPreferences,
                        onCheckedChange = {
                            onIntent(SettingsIntent.ToggleNotificationPreference(NotificationPreferenceKey.ListChanges, it))
                        },
                    )
                    NotificationSwitchRow(
                        label = stringResource(Res.string.settings_notify_member_changes),
                        checked = state.notificationPreferences.memberChanges,
                        enabled = !state.isSavingNotificationPreferences,
                        onCheckedChange = {
                            onIntent(SettingsIntent.ToggleNotificationPreference(NotificationPreferenceKey.MemberChanges, it))
                        },
                    )
                    if (state.notificationPreferencesError) {
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = stringResource(Res.string.settings_notifications_error),
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodySmall,
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            OutlinedButton(
                onClick = { onIntent(SettingsIntent.ShowLeaveConfirmation) },
                modifier = Modifier.fillMaxWidth().height(56.dp),
                shape = RoundedCornerShape(28.dp),
                colors = ButtonDefaults.outlinedButtonColors(
                    contentColor = MaterialTheme.colorScheme.error,
                ),
            ) {
                Text(stringResource(Res.string.settings_leave_household))
            }
            Spacer(modifier = Modifier.height(8.dp))

            OutlinedButton(
                onClick = { onIntent(SettingsIntent.SignOut) },
                modifier = Modifier.fillMaxWidth().height(56.dp),
                shape = RoundedCornerShape(28.dp),
                colors = ButtonDefaults.outlinedButtonColors(
                    contentColor = MaterialTheme.colorScheme.error,
                ),
            ) {
                Icon(Icons.AutoMirrored.Filled.ExitToApp, contentDescription = null)
                Spacer(modifier = Modifier.width(8.dp))
                Text(stringResource(Res.string.settings_sign_out))
            }

            if (state.showLeaveConfirmation) {
                AlertDialog(
                    onDismissRequest = { onIntent(SettingsIntent.DismissLeaveConfirmation) },
                    title = { Text(stringResource(Res.string.settings_leave_title)) },
                    text = { Text(stringResource(Res.string.settings_leave_message, state.householdName)) },
                    confirmButton = {
                        TextButton(
                            onClick = { onIntent(SettingsIntent.ConfirmLeaveHousehold) },
                            colors = ButtonDefaults.textButtonColors(contentColor = MaterialTheme.colorScheme.error),
                        ) { Text(stringResource(Res.string.settings_leave)) }
                    },
                    dismissButton = {
                        TextButton(onClick = { onIntent(SettingsIntent.DismissLeaveConfirmation) }) { Text(stringResource(Res.string.cancel)) }
                    },
                )
            }

            if (state.showRenameDialog) {
                AlertDialog(
                    onDismissRequest = { onIntent(SettingsIntent.DismissRenameDialog) },
                    title = { Text(stringResource(Res.string.settings_rename_household)) },
                    text = {
                        val focusRequester = remember { FocusRequester() }
                        val textFieldValue = remember(state.showRenameDialog) {
                            mutableStateOf(
                                TextFieldValue(
                                    text = state.renameText,
                                    selection = TextRange(0, state.renameText.length),
                                ),
                            )
                        }
                        OutlinedTextField(
                            value = textFieldValue.value,
                            onValueChange = {
                                textFieldValue.value = it
                                onIntent(SettingsIntent.UpdateRenameText(it.text))
                            },
                            label = { Text(stringResource(Res.string.settings_household_name_label)) },
                            singleLine = true,
                            modifier = Modifier
                                .fillMaxWidth()
                                .focusRequester(focusRequester)
                                .testTag("Rename household input"),
                        )
                        LaunchedEffect(Unit) {
                            focusRequester.requestFocus()
                        }
                    },
                    confirmButton = {
                        TextButton(
                            onClick = { onIntent(SettingsIntent.ConfirmRename) },
                            enabled = state.renameText.isNotBlank() && !state.isRenaming,
                        ) { Text(stringResource(Res.string.rename)) }
                    },
                    dismissButton = {
                        TextButton(onClick = { onIntent(SettingsIntent.DismissRenameDialog) }) { Text(stringResource(Res.string.cancel)) }
                    },
                )
            }
        }
    }
}

@Composable
private fun NotificationSwitchRow(
    label: String,
    checked: Boolean,
    enabled: Boolean,
    onCheckedChange: (Boolean) -> Unit,
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .heightIn(min = 48.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(
            text = label,
            style = MaterialTheme.typography.bodyMedium,
            modifier = Modifier.weight(1f),
        )
        Switch(
            checked = checked,
            enabled = enabled,
            onCheckedChange = onCheckedChange,
        )
    }
}
