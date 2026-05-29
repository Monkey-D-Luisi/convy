package com.convy.app.ui.screens.activity

import com.convy.app.generated.resources.Res
import com.convy.app.generated.resources.activity_deleted
import com.convy.shared.domain.model.ActivityLogEntry
import kotlin.test.Test
import kotlin.test.assertEquals

class ActivityTextTest {
    @Test
    fun `deleted item includes metadata title`() {
        val text = activityActionText(activityEntry(entityType = "Item", actionType = "Deleted", metadata = "Milk"))

        assertEquals(Res.string.activity_deleted, text.resource)
        assertEquals(listOf("item", " \"Milk\""), text.args)
    }

    @Test
    fun `blank metadata is omitted`() {
        val text = activityActionText(activityEntry(entityType = "Task", actionType = "Deleted", metadata = ""))

        assertEquals(listOf("task", ""), text.args)
    }

    private fun activityEntry(
        entityType: String,
        actionType: String,
        metadata: String?,
    ) = ActivityLogEntry(
        id = "activity-1",
        householdId = "household-1",
        entityType = entityType,
        entityId = "entity-1",
        actionType = actionType,
        performedBy = "user-1",
        performedByName = "Test User",
        createdAt = "2026-05-29T10:00:00Z",
        metadata = metadata,
    )
}
