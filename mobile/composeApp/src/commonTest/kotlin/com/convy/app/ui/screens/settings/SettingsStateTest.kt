package com.convy.app.ui.screens.settings

import kotlin.test.Test
import kotlin.test.assertEquals

class SettingsStateTest {
    @Test
    fun `should expose app version from state`() {
        val state = SettingsState(appVersion = "0.1.4")

        assertEquals("0.1.4", state.appVersion)
    }

    @Test
    fun `should use high signal notification defaults`() {
        val state = SettingsState()

        assertEquals(true, state.notificationPreferences.itemsAdded)
        assertEquals(true, state.notificationPreferences.tasksAdded)
        assertEquals(false, state.notificationPreferences.itemsCompleted)
        assertEquals(false, state.notificationPreferences.tasksCompleted)
        assertEquals(false, state.notificationPreferences.itemTaskChanges)
        assertEquals(true, state.notificationPreferences.listChanges)
        assertEquals(true, state.notificationPreferences.memberChanges)
    }
}
