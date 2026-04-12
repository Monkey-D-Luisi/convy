package com.convy.app.ui.screens.auth

import com.convy.app.util.UiText

data class AuthState(
    val email: String = "",
    val password: String = "",
    val displayName: String = "",
    val isSignUp: Boolean = false,
    val isLoading: Boolean = false,
    val error: UiText? = null,
    val emailError: UiText? = null,
    val passwordError: UiText? = null,
    val nameError: UiText? = null,
)

sealed interface AuthIntent {
    data class UpdateEmail(val email: String) : AuthIntent
    data class UpdatePassword(val password: String) : AuthIntent
    data class UpdateDisplayName(val name: String) : AuthIntent
    data object ToggleMode : AuthIntent
    data object Submit : AuthIntent
    data object GoogleSignIn : AuthIntent
}

sealed interface AuthSideEffect {
    data class NavigateToHouseholdSetup(val userId: String) : AuthSideEffect
    data class NavigateToLists(val householdId: String) : AuthSideEffect
}
