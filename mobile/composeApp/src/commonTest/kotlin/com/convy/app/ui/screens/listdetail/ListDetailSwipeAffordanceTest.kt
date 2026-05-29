package com.convy.app.ui.screens.listdetail

import androidx.compose.material3.SwipeToDismissBoxValue
import kotlin.test.Test
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class ListDetailSwipeAffordanceTest {
    @Test
    fun `delete affordance is hidden while swipe is settled`() {
        assertFalse(shouldShowDeleteSwipeAffordance(SwipeToDismissBoxValue.Settled))
    }

    @Test
    fun `delete affordance is visible for end to start swipe target`() {
        assertTrue(shouldShowDeleteSwipeAffordance(SwipeToDismissBoxValue.EndToStart))
    }
}
