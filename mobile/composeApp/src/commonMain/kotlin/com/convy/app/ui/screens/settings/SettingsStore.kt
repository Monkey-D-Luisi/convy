package com.convy.app.ui.screens.settings

import com.convy.shared.domain.repository.AuthRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class SettingsStore(
    private val authRepository: AuthRepository,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val _state = MutableStateFlow(SettingsState())
    val state: StateFlow<SettingsState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<SettingsSideEffect>()
    val sideEffects: SharedFlow<SettingsSideEffect> = _sideEffects.asSharedFlow()

    init {
        loadProfile()
    }

    fun processIntent(intent: SettingsIntent) {
        when (intent) {
            is SettingsIntent.SignOut -> signOut()
            is SettingsIntent.NavigateBack -> scope.launch {
                _sideEffects.emit(SettingsSideEffect.NavigateBack)
            }
        }
    }

    private fun loadProfile() {
        scope.launch {
            val user = authRepository.getCurrentUser()
            if (user != null) {
                _state.update {
                    it.copy(
                        displayName = user.displayName,
                        email = user.email,
                    )
                }
            }
        }
    }

    private fun signOut() {
        scope.launch {
            authRepository.signOut()
            _sideEffects.emit(SettingsSideEffect.NavigateToAuth)
        }
    }
}
