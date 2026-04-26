package com.convy.app.ui.screens.listdetail

import com.convy.app.generated.resources.*
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
    fun `task lists cannot enter shopping mode`() {
        val transition = ListDetailState(listType = "Tasks").toggleShoppingMode()

        assertFalse(transition.state.isShoppingMode)
        assertFalse(transition.shouldReloadItems)
    }

    @Test
    fun `completion exit rows are tracked by id`() {
        val state = ListDetailState(completionExitEntryIds = setOf("entry-1"))

        assertTrue("entry-1" in state.completionExitEntryIds)
    }

    @Test
    fun `entering shopping mode from filtered list should request reload`() {
        val transition = ListDetailState(
            activeFilter = "Pending",
            isSearching = true,
            searchQuery = "milk",
        ).toggleShoppingMode()

        assertTrue(transition.state.isShoppingMode)
        assertFalse(transition.state.isSearching)
        assertEquals("", transition.state.searchQuery)
        assertEquals("All", transition.state.activeFilter)
        assertTrue(transition.shouldReloadItems)
    }

    @Test
    fun `entering shopping mode from all items should not request reload`() {
        val transition = ListDetailState(activeFilter = "All").toggleShoppingMode()

        assertTrue(transition.state.isShoppingMode)
        assertEquals("All", transition.state.activeFilter)
        assertFalse(transition.shouldReloadItems)
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
