package com.convy.app.navigation

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class AppNavigator {
    private val backStack = mutableListOf<NavRoute>()
    private val _currentRoute = MutableStateFlow<NavRoute>(NavRoute.Auth)
    val currentRoute: StateFlow<NavRoute> = _currentRoute.asStateFlow()

    fun navigateTo(route: NavRoute) {
        backStack.add(_currentRoute.value)
        _currentRoute.value = route
    }

    fun navigateBack(): Boolean {
        if (backStack.isEmpty()) return false
        _currentRoute.value = backStack.removeAt(backStack.lastIndex)
        return true
    }

    fun replaceWith(route: NavRoute) {
        backStack.clear()
        _currentRoute.value = route
    }
}
