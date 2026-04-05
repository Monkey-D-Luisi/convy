package com.convy.app.ui.screens.householdsetup

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@Composable
fun HouseholdSetupScreen(
    store: HouseholdSetupStore,
    onNavigateToLists: (String) -> Unit,
) {
    val state by store.state.collectAsState()

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
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        Text(
            text = "Set up your home",
            style = MaterialTheme.typography.headlineMedium,
        )
        Spacer(modifier = Modifier.height(8.dp))
        Text(
            text = if (state.isCreateMode) "Create a new household to get started"
            else "Enter an invite code to join an existing household",
            style = MaterialTheme.typography.bodyLarge,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )

        Spacer(modifier = Modifier.height(32.dp))

        if (state.isCreateMode) {
            OutlinedTextField(
                value = state.householdName,
                onValueChange = { onIntent(HouseholdSetupIntent.UpdateHouseholdName(it)) },
                label = { Text("Household name") },
                placeholder = { Text("e.g. Our Home") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
            )
        } else {
            OutlinedTextField(
                value = state.inviteCode,
                onValueChange = { onIntent(HouseholdSetupIntent.UpdateInviteCode(it)) },
                label = { Text("Invite code") },
                placeholder = { Text("Enter the code you received") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
            )
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

        Button(
            onClick = { onIntent(HouseholdSetupIntent.Submit) },
            modifier = Modifier.fillMaxWidth().height(48.dp),
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
                Text(if (state.isCreateMode) "Create household" else "Join household")
            }
        }

        Spacer(modifier = Modifier.height(12.dp))

        TextButton(onClick = { onIntent(HouseholdSetupIntent.ToggleMode) }) {
            Text(
                if (state.isCreateMode) "Have an invite code? Join instead"
                else "Want to create a new household?",
            )
        }
    }
}
