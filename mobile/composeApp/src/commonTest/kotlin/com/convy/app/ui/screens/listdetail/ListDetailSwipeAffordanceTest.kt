package com.convy.app.ui.screens.listdetail

import androidx.compose.material3.SnackbarResult
import androidx.compose.material3.SwipeToDismissBoxValue
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertNull
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

    @Test
    fun `end to start swipe requests delete without accepting dismiss`() {
        var requestCount = 0

        val accepted = confirmDeleteSwipeRequest(SwipeToDismissBoxValue.EndToStart) {
            requestCount += 1
        }

        assertFalse(accepted)
        assertEquals(1, requestCount)
    }

    @Test
    fun `settled swipe target does not request delete and keeps dismiss rejected`() {
        var requestCount = 0

        val accepted = confirmDeleteSwipeRequest(SwipeToDismissBoxValue.Settled) {
            requestCount += 1
        }

        assertFalse(accepted)
        assertEquals(0, requestCount)
    }

    @Test
    fun `confirmation snackbar action maps to delete intent`() {
        assertEquals(
            ListDetailIntent.DeleteItem("item-1"),
            deleteConfirmationIntentForSnackbarResult(SnackbarResult.ActionPerformed, "item-1"),
        )
    }

    @Test
    fun `dismissed confirmation snackbar maps to no intent`() {
        assertNull(deleteConfirmationIntentForSnackbarResult(SnackbarResult.Dismissed, "item-1"))
    }
}
