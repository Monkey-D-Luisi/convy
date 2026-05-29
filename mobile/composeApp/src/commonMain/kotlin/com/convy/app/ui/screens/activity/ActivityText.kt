package com.convy.app.ui.screens.activity

import com.convy.app.generated.resources.Res
import com.convy.app.generated.resources.activity_archived
import com.convy.app.generated.resources.activity_completed
import com.convy.app.generated.resources.activity_created
import com.convy.app.generated.resources.activity_default
import com.convy.app.generated.resources.activity_deleted
import com.convy.app.generated.resources.activity_member_joined
import com.convy.app.generated.resources.activity_renamed
import com.convy.app.generated.resources.activity_uncompleted
import com.convy.app.generated.resources.activity_updated
import com.convy.shared.domain.model.ActivityLogEntry
import org.jetbrains.compose.resources.StringResource

internal data class ActivityActionText(
    val resource: StringResource,
    val args: List<String> = emptyList(),
)

internal fun activityActionText(entry: ActivityLogEntry): ActivityActionText {
    val entityLabel = entry.entityType.lowercase()
    val metadata = entry.metadata
        ?.takeIf { it.isNotBlank() }
        ?.let { " \"$it\"" }
        ?: ""

    return when (entry.actionType) {
        "Created" -> ActivityActionText(Res.string.activity_created, listOf(entityLabel, metadata))
        "Updated" -> ActivityActionText(Res.string.activity_updated, listOf(entityLabel, metadata))
        "Completed" -> ActivityActionText(Res.string.activity_completed, listOf(entityLabel, metadata))
        "Uncompleted" -> ActivityActionText(Res.string.activity_uncompleted, listOf(entityLabel, metadata))
        "Deleted" -> ActivityActionText(Res.string.activity_deleted, listOf(entityLabel, metadata))
        "Archived" -> ActivityActionText(Res.string.activity_archived, listOf(entityLabel, metadata))
        "Renamed" -> ActivityActionText(Res.string.activity_renamed, listOf(entityLabel, metadata))
        "MemberJoined" -> ActivityActionText(Res.string.activity_member_joined)
        else -> ActivityActionText(Res.string.activity_default, listOf(entityLabel, entry.actionType.lowercase()))
    }
}
