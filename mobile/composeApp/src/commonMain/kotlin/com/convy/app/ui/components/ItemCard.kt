package com.convy.app.ui.components

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.clickable
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.convy.app.generated.resources.Res
import com.convy.app.generated.resources.item_card_added_by
import com.convy.app.generated.resources.item_card_completed_by
import com.convy.app.generated.resources.item_card_completed_by_at
import com.convy.app.generated.resources.item_card_recurring
import com.convy.app.generated.resources.item_card_returned_to_pending_by_at
import com.convy.app.generated.resources.unknown
import com.convy.shared.domain.model.ListItem
import org.jetbrains.compose.resources.stringResource

@Composable
fun ItemCard(
    item: ListItem,
    onToggleComplete: () -> Unit,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
) {
    Card(
        modifier = modifier.fillMaxWidth().clickable(onClick = onClick),
        shape = MaterialTheme.shapes.large,
        colors = CardDefaults.cardColors(
            containerColor = if (item.isCompleted) {
                MaterialTheme.colorScheme.surfaceContainerHighest.copy(alpha = 0.72f)
            } else {
                MaterialTheme.colorScheme.surface.copy(alpha = 0.96f)
            },
        ),
        elevation = CardDefaults.cardElevation(defaultElevation = if (item.isCompleted) 1.dp else 3.dp),
    ) {
        Row(
            modifier = Modifier.padding(start = 8.dp, end = 16.dp, top = 14.dp, bottom = 14.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Checkbox(
                checked = item.isCompleted,
                onCheckedChange = { onToggleComplete() },
                modifier = Modifier.testTag("item-checkbox"),
                colors = CheckboxDefaults.colors(
                    checkedColor = MaterialTheme.colorScheme.primary,
                ),
            )
            Column(modifier = Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        text = item.title,
                        style = MaterialTheme.typography.titleMedium.copy(
                            textDecoration = if (item.isCompleted) TextDecoration.LineThrough else TextDecoration.None,
                        ),
                        color = if (item.isCompleted) {
                            MaterialTheme.colorScheme.onSurfaceVariant
                        } else {
                            MaterialTheme.colorScheme.onSurface
                        },
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                        modifier = Modifier.weight(1f, fill = false),
                    )
                    if (item.recurrenceFrequency != null) {
                        Spacer(modifier = Modifier.width(6.dp))
                        Icon(
                            Icons.Default.Refresh,
                            contentDescription = stringResource(Res.string.item_card_recurring),
                            modifier = Modifier.size(16.dp),
                            tint = MaterialTheme.colorScheme.primary,
                        )
                    }
                }
                val details = buildList {
                    if (item.quantity != null) {
                        add("${item.quantity}${item.unit?.let { " $it" } ?: ""}")
                    }
                    if (item.note != null) {
                        add(item.note)
                    }
                }
                if (details.isNotEmpty()) {
                    Spacer(modifier = Modifier.height(3.dp))
                    Text(
                        text = details.joinToString(" / "),
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                    )
                }
                Spacer(modifier = Modifier.height(3.dp))
                Text(
                    text = if (item.isCompleted) {
                        item.completedAt?.let {
                            stringResource(
                                Res.string.item_card_completed_by_at,
                                item.completedByName ?: stringResource(Res.string.unknown),
                                formatTimestamp(it),
                            )
                        }
                            ?: stringResource(Res.string.item_card_completed_by, item.completedByName ?: stringResource(Res.string.unknown))
                    } else if (item.returnedToPendingAt != null) {
                        val returnedToPendingAt = item.returnedToPendingAt ?: item.createdAt
                        stringResource(
                            Res.string.item_card_returned_to_pending_by_at,
                            item.returnedToPendingByName ?: stringResource(Res.string.unknown),
                            formatTimestamp(returnedToPendingAt),
                        )
                    } else {
                        stringResource(Res.string.item_card_added_by, item.createdByName, formatTimestamp(item.createdAt))
                    },
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            Spacer(modifier = Modifier.width(10.dp))
            ConvyAvatar(
                label = item.completedByName ?: item.createdByName,
                containerColor = MaterialTheme.colorScheme.primaryContainer.copy(alpha = if (item.isCompleted) 0.38f else 0.58f),
            )
        }
    }
}

private fun formatTimestamp(iso: String): String {
    return iso.take(16).replace("T", " ")
}
