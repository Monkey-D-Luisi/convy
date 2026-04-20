package com.convy.app.ui.screens.members

import kotlin.test.Test
import kotlin.test.assertEquals

class MembersIntentTest {
    @Test
    fun `should copy invite by identifier`() {
        val intent = MembersIntent.CopyInviteCode("invite-123")

        assertEquals("invite-123", intent.inviteId)
    }
}
