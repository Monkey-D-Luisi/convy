package com.convy.app.ui.screens.listdetail

import com.convy.app.generated.resources.Res
import com.convy.app.generated.resources.detail_voice_permission_required
import com.convy.app.util.UiText
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class ListDetailStateTest {
    @Test
    fun `should show normal list chrome outside shopping mode`() {
        assertTrue(ListDetailState(isShoppingMode = false).showNormalListChrome)
    }

    @Test
    fun `should hide normal list chrome in shopping mode`() {
        assertFalse(ListDetailState(isShoppingMode = true).showNormalListChrome)
    }

    @Test
    fun `should ignore voice item toggle outside list bounds`() {
        val items = listOf(ParsedVoiceItem("Milk", 1, "bottle"))

        assertEquals(items, items.toggleSelectionAt(4))
    }

    @Test
    fun `voice permission error should use string resource`() {
        val effect = ListDetailSideEffect.ShowError(
            UiText.StringResourceText(Res.string.detail_voice_permission_required)
        )

        val message = effect.message as UiText.StringResourceText
        assertEquals(Res.string.detail_voice_permission_required, message.res)
    }
}
