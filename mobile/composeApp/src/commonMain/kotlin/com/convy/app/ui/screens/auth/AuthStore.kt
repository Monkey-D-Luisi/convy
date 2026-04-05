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
            is AuthIntent.UpdateEmail -> _state.update { it.copy(email = intent.email) }
            is AuthIntent.UpdatePassword -> _state.update { it.copy(password = intent.password) }
            is AuthIntent.UpdateDisplayName -> _state.update { it.copy(displayName = intent.name) }
            is AuthIntent.ToggleMode -> _state.update { it.copy(isSignUp = !it.isSignUp, error = null) }
            is AuthIntent.Submit -> submit()
        }
    }

    private fun submit() {
        val current = _state.value
        if (current.isLoading) return

        _state.update { it.copy(isLoading = true, error = null) }

        scope.launch {
            val result = if (current.isSignUp) {
                authRepository.signUp(current.email, current.password, current.displayName)
            } else {
                authRepository.signIn(current.email, current.password)
            }

            result.fold(
                onSuccess = { user ->
                    val token = authRepository.getIdToken()
                    if (token != null) {
                        userRepository.register(token, user.displayName, user.email)
                    }
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
}
