package com.convy.app.navigation

import kotlin.test.Test
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class AppNavigatorTest {
    @Test
    fun `can navigate back tracks stack availability`() {
        val navigator = AppNavigator()

        assertFalse(navigator.canNavigateBack.value)

        navigator.navigateTo(NavRoute.HouseholdSetup)

        assertTrue(navigator.canNavigateBack.value)

        navigator.navigateBack()

        assertFalse(navigator.canNavigateBack.value)
    }

    @Test
    fun `replace clears back stack availability`() {
        val navigator = AppNavigator()
        navigator.navigateTo(NavRoute.HouseholdSetup)

        navigator.replaceWith(NavRoute.Auth)

        assertFalse(navigator.canNavigateBack.value)
    }
}
