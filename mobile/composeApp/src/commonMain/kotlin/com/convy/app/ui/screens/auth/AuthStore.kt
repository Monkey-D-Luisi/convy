package com.convy.app.ui.screens.auth

import com.convy.shared.domain.repository.AuthRepository
import com.convy.shared.domain.repository.HouseholdRepository
import com.convy.shared.domain.repository.UserRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class AuthStore(
    private val authRepository: AuthRepository,
    private val userRepository: UserRepository,
    private val householdRepository: HouseholdRepository,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val _state = MutableStateFlow(AuthState())
    val state: StateFlow<AuthState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<AuthSideEffect>()
    val sideEffects: SharedFlow<AuthSideEffect> = _sideEffects.asSharedFlow()

    fun processIntent(intent: AuthIntent) {
        when (intent) {
            is AuthIntent.UpdateEmail -> _state.update { it.copy(email = intent.email, emailError = null) }
            is AuthIntent.UpdatePassword -> _state.update { it.copy(password = intent.password, passwordError = null) }
            is AuthIntent.UpdateDisplayName -> _state.update { it.copy(displayName = intent.name, nameError = null) }
            is AuthIntent.ToggleMode -> _state.update { it.copy(isSignUp = !it.isSignUp, error = null) }
            is AuthIntent.Submit -> submit()
            is AuthIntent.GoogleSignIn -> signInWithGoogle()
        }
    }

    private fun validate(): Boolean {
        val current = _state.value

        val emailError = when {
            current.email.isBlank() -> "Email is required"
            !current.email.contains("@") -> "Invalid email format"
            else -> null
        }

        val passwordError = when {
            current.password.isBlank() -> "Password is required"
            current.password.length < 6 -> "Password must be at least 6 characters"
            else -> null
        }

        val nameError = if (current.isSignUp && current.displayName.isBlank()) {
            "Display name is required"
        } else {
            null
        }

        _state.update { it.copy(emailError = emailError, passwordError = passwordError, nameError = nameError) }
        return emailError == null && passwordError == null && nameError == null
    }

    private fun submit() {
        val current = _state.value
        if (current.isLoading) return
        if (!validate()) return

        _state.update { it.copy(isLoading = true, error = null) }

        scope.launch {
            val result = if (current.isSignUp) {
                authRepository.signUp(current.email, current.password, current.displayName)
            } else {
                authRepository.signIn(current.email, current.password)
            }

            result.fold(
                onSuccess = { user ->
                    userRepository.register(user.id, user.displayName, user.email)
                    val households = householdRepository.getMyHouseholds().getOrNull()
                    _state.update { it.copy(isLoading = false) }
                    if (households.isNullOrEmpty()) {
                        _sideEffects.emit(AuthSideEffect.NavigateToHouseholdSetup(user.id))
                    } else {
                        _sideEffects.emit(AuthSideEffect.NavigateToLists(households.first().id))
                    }
                },
                onFailure = { error ->
                    _state.update { it.copy(isLoading = false, error = error.message ?: "Authentication failed") }
                },
            )
        }
    }

    private fun signInWithGoogle() {
        if (_state.value.isLoading) return

        _state.update { it.copy(isLoading = true, error = null) }

        scope.launch {
            val result = authRepository.signInWithGoogle()

            result.fold(
                onSuccess = { user ->
                    userRepository.register(user.id, user.displayName, user.email)
                    val households = householdRepository.getMyHouseholds().getOrNull()
                    _state.update { it.copy(isLoading = false) }
                    if (households.isNullOrEmpty()) {
                        _sideEffects.emit(AuthSideEffect.NavigateToHouseholdSetup(user.id))
                    } else {
                        _sideEffects.emit(AuthSideEffect.NavigateToLists(households.first().id))
                    }
                },
                onFailure = { error ->
                    _state.update { it.copy(isLoading = false, error = error.message ?: "Google sign-in failed") }
                },
            )
        }
    }
}
