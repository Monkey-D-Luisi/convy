package com.convy.app.ui.screens.auth

data class AuthState(
    val email: String = "",
    val password: String = "",
    val displayName: String = "",
    val isSignUp: Boolean = false,
    val isLoading: Boolean = false,
    val error: String? = null,
    val emailError: String? = null,
    val passwordError: String? = null,
    val nameError: String? = null,
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
