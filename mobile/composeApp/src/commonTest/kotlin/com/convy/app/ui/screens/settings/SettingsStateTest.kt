package com.convy.app.ui.screens.settings

import kotlin.test.Test
import kotlin.test.assertEquals

class SettingsStateTest {
    @Test
    fun `should expose app version from state`() {
        val state = SettingsState(appVersion = "0.1.4")

        assertEquals("0.1.4", state.appVersion)
    }
}
