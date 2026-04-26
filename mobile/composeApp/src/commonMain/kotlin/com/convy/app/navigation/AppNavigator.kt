package com.convy.app.navigation

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class AppNavigator {
    private val backStack = mutableListOf<NavRoute>()
    private val _currentRoute = MutableStateFlow<NavRoute>(NavRoute.Auth)
    private val _canNavigateBack = MutableStateFlow(false)
    val currentRoute: StateFlow<NavRoute> = _currentRoute.asStateFlow()
    val canNavigateBack: StateFlow<Boolean> = _canNavigateBack.asStateFlow()

    fun navigateTo(route: NavRoute) {
        backStack.add(_currentRoute.value)
        _currentRoute.value = route
        _canNavigateBack.value = backStack.isNotEmpty()
    }

    fun navigateBack(): Boolean {
        if (backStack.isEmpty()) return false
        _currentRoute.value = backStack.removeAt(backStack.lastIndex)
        _canNavigateBack.value = backStack.isNotEmpty()
        return true
    }

    fun replaceWith(route: NavRoute) {
        backStack.clear()
        _currentRoute.value = route
        _canNavigateBack.value = false
    }
}
