package com.convy.app.ui.screens.householdsetup

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Home
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.convy.app.generated.resources.*
import com.convy.app.ui.components.ConvyBackground
import com.convy.app.ui.components.ConvyIconBubble
import com.convy.app.ui.components.ConvyPanel
import com.convy.app.ui.components.ConvyPrimaryButton
import com.convy.app.ui.components.ConvySpacing
import com.convy.app.ui.components.convyTextFieldColors
import org.jetbrains.compose.resources.stringResource

@Composable
fun HouseholdSetupScreen(
    store: HouseholdSetupStore,
    onNavigateToLists: (String) -> Unit,
) {
    val state by store.state.collectAsState()

    DisposableEffect(store) {
        onDispose { store.close() }
    }

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is HouseholdSetupSideEffect.NavigateToLists -> onNavigateToLists(effect.householdId)
            }
        }
    }

    HouseholdSetupContent(state = state, onIntent = store::processIntent)
}

@Composable
fun HouseholdSetupContent(
    state: HouseholdSetupState,
    onIntent: (HouseholdSetupIntent) -> Unit,
) {
    ConvyBackground {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = ConvySpacing.ScreenHorizontal)
                .padding(top = 34.dp, bottom = 24.dp),
            horizontalAlignment = Alignment.Start,
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                ConvyIconBubble(
                    icon = Icons.Default.Home,
                    contentDescription = null,
                    size = 44.dp,
                    iconSize = 22.dp,
                    containerColor = MaterialTheme.colorScheme.primaryContainer.copy(alpha = 0.76f),
                )
                Spacer(modifier = Modifier.width(12.dp))
                Text(
                    text = "Convy",
                    style = MaterialTheme.typography.titleLarge,
                    color = MaterialTheme.colorScheme.onSurface,
                )
            }
            Spacer(modifier = Modifier.height(42.dp))
            Text(
                text = stringResource(Res.string.setup_title),
                style = MaterialTheme.typography.headlineLarge,
                color = MaterialTheme.colorScheme.onSurface,
            )
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = if (state.isCreateMode) stringResource(Res.string.setup_create_subtitle)
                else stringResource(Res.string.setup_join_subtitle),
                style = MaterialTheme.typography.bodyLarge,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Spacer(modifier = Modifier.height(28.dp))

            ConvyPanel(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(18.dp)) {
                    if (state.isCreateMode) {
                        TextField(
                            value = state.householdName,
                            onValueChange = { onIntent(HouseholdSetupIntent.UpdateHouseholdName(it)) },
                            label = { Text(stringResource(Res.string.setup_household_name)) },
                            placeholder = { Text(stringResource(Res.string.setup_household_placeholder)) },
                            leadingIcon = {
                                Icon(Icons.Default.Home, contentDescription = null)
                            },
                            singleLine = true,
                            modifier = Modifier.fillMaxWidth(),
                            shape = MaterialTheme.shapes.large,
                            colors = convyTextFieldColors(),
                        )
                        Spacer(modifier = Modifier.height(6.dp))
                        Text(
                            text = stringResource(Res.string.setup_household_hint),
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            modifier = Modifier.fillMaxWidth(),
                        )
                    } else {
                        TextField(
                            value = state.inviteCode,
                            onValueChange = { onIntent(HouseholdSetupIntent.UpdateInviteCode(it)) },
                            label = { Text(stringResource(Res.string.setup_invite_code)) },
                            placeholder = { Text(stringResource(Res.string.setup_invite_placeholder)) },
                            singleLine = true,
                            modifier = Modifier.fillMaxWidth(),
                            shape = MaterialTheme.shapes.large,
                            colors = convyTextFieldColors(),
                        )
                        Spacer(modifier = Modifier.height(6.dp))
                        Text(
                            text = stringResource(Res.string.setup_invite_hint),
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            modifier = Modifier.fillMaxWidth(),
                        )
                    }

                    if (state.error != null) {
                        Spacer(modifier = Modifier.height(12.dp))
                        Text(
                            text = state.error.asString(),
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodySmall,
                        )
                    }

                    Spacer(modifier = Modifier.height(22.dp))

                    ConvyPrimaryButton(
                        onClick = { onIntent(HouseholdSetupIntent.Submit) },
                        modifier = Modifier.fillMaxWidth(),
                        enabled = !state.isLoading && if (state.isCreateMode) {
                            state.householdName.isNotBlank()
                        } else {
                            state.inviteCode.isNotBlank()
                        },
                    ) {
                        if (state.isLoading) {
                            CircularProgressIndicator(
                                modifier = Modifier.size(20.dp),
                                strokeWidth = 2.dp,
                                color = MaterialTheme.colorScheme.onPrimary,
                            )
                        } else {
                            Text(if (state.isCreateMode) stringResource(Res.string.setup_create_household) else stringResource(Res.string.setup_join_household))
                        }
                    }
                }
            }
            Spacer(modifier = Modifier.height(12.dp))

            TextButton(
                onClick = { onIntent(HouseholdSetupIntent.ToggleMode) },
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(
                    if (state.isCreateMode) stringResource(Res.string.setup_toggle_to_join)
                    else stringResource(Res.string.setup_toggle_to_create),
                    style = MaterialTheme.typography.labelLarge,
                    color = MaterialTheme.colorScheme.primary,
                )
            }
        }
    }
}
