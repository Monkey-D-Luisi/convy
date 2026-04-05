package com.convy.app.ui.screens.auth

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp

@Composable
fun AuthScreen(
    store: AuthStore,
    onNavigateToHouseholdSetup: () -> Unit,
    onNavigateToLists: (String) -> Unit,
) {
    val state by store.state.collectAsState()

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is AuthSideEffect.NavigateToHouseholdSetup -> onNavigateToHouseholdSetup()
                is AuthSideEffect.NavigateToLists -> onNavigateToLists(effect.householdId)
            }
        }
    }

    AuthContent(state = state, onIntent = store::processIntent)
}

@Composable
fun AuthContent(
    state: AuthState,
    onIntent: (AuthIntent) -> Unit,
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        Text(
            text = "Convy",
            style = MaterialTheme.typography.displayMedium,
            color = MaterialTheme.colorScheme.primary,
        )
        Spacer(modifier = Modifier.height(8.dp))
        Text(
            text = "Coordinate your home effortlessly",
            style = MaterialTheme.typography.bodyLarge,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )

        Spacer(modifier = Modifier.height(48.dp))

        if (state.isSignUp) {
            OutlinedTextField(
                value = state.displayName,
                onValueChange = { onIntent(AuthIntent.UpdateDisplayName(it)) },
                label = { Text("Display name") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
                keyboardOptions = KeyboardOptions(imeAction = ImeAction.Next),
            )
            Spacer(modifier = Modifier.height(12.dp))
        }

        OutlinedTextField(
            value = state.email,
            onValueChange = { onIntent(AuthIntent.UpdateEmail(it)) },
            label = { Text("Email") },
            singleLine = true,
            modifier = Modifier.fillMaxWidth(),
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Email,
                imeAction = ImeAction.Next,
            ),
        )
        Spacer(modifier = Modifier.height(12.dp))

        OutlinedTextField(
            value = state.password,
            onValueChange = { onIntent(AuthIntent.UpdatePassword(it)) },
            label = { Text("Password") },
            singleLine = true,
            modifier = Modifier.fillMaxWidth(),
            visualTransformation = PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Password,
                imeAction = ImeAction.Done,
            ),
        )

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
            onClick = { onIntent(AuthIntent.Submit) },
            modifier = Modifier.fillMaxWidth().height(48.dp),
            enabled = !state.isLoading,
        ) {
            if (state.isLoading) {
                CircularProgressIndicator(
                    modifier = Modifier.size(20.dp),
                    strokeWidth = 2.dp,
                    color = MaterialTheme.colorScheme.onPrimary,
                )
            } else {
                Text(if (state.isSignUp) "Create account" else "Sign in")
            }
        }

        Spacer(modifier = Modifier.height(12.dp))

        TextButton(onClick = { onIntent(AuthIntent.ToggleMode) }) {
            Text(
                if (state.isSignUp) "Already have an account? Sign in"
                else "Don't have an account? Create one",
            )
        }
    }
}
