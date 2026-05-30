package com.convy.app.ui.components

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.clickable
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.convy.app.generated.resources.Res
import com.convy.app.generated.resources.archive
import com.convy.app.generated.resources.lists_type_shopping
import com.convy.app.generated.resources.lists_type_tasks
import com.convy.app.generated.resources.options
import com.convy.app.generated.resources.rename
import com.convy.shared.domain.model.HouseholdList
import com.convy.shared.domain.model.ListType
import org.jetbrains.compose.resources.stringResource

@Composable
fun ListCard(
    list: HouseholdList,
    pendingCount: Int,
    onClick: () -> Unit,
    onRenameClick: (() -> Unit)? = null,
    onArchiveClick: (() -> Unit)? = null,
    modifier: Modifier = Modifier,
) {
    val accent = when (list.type) {
        ListType.Shopping -> MaterialTheme.colorScheme.primary
        ListType.Tasks -> MaterialTheme.colorScheme.secondary
    }
    Card(
        modifier = modifier.fillMaxWidth().clickable(onClick = onClick),
        shape = MaterialTheme.shapes.large,
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surface,
        ),
        elevation = CardDefaults.cardElevation(defaultElevation = 0.dp),
        border = BorderStroke(1.dp, MaterialTheme.colorScheme.outlineVariant.copy(alpha = 0.64f)),
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(14.dp),
        ) {
            ConvyIconBubble(
                icon = when (list.type) {
                    ListType.Shopping -> Icons.Default.ShoppingCart
                    ListType.Tasks -> Icons.Default.CheckCircle
                },
                contentDescription = null,
                tint = accent,
                containerColor = accent.copy(alpha = 0.12f),
                size = 48.dp,
                iconSize = 24.dp,
            )
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = list.name,
                    style = MaterialTheme.typography.titleMedium,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
                Surface(
                    shape = MaterialTheme.shapes.extraLarge,
                    color = accent.copy(alpha = 0.11f),
                    modifier = Modifier.padding(top = 8.dp),
                ) {
                    Text(
                        text = when (list.type) {
                            ListType.Shopping -> stringResource(Res.string.lists_type_shopping)
                            ListType.Tasks -> stringResource(Res.string.lists_type_tasks)
                        },
                        style = MaterialTheme.typography.labelMedium,
                        color = accent,
                        modifier = Modifier.padding(horizontal = 10.dp, vertical = 5.dp),
                    )
                }
            }
            if (pendingCount > 0) {
                Surface(
                    shape = MaterialTheme.shapes.extraLarge,
                    color = accent.copy(alpha = 0.13f),
                    contentColor = accent,
                    modifier = Modifier.size(38.dp),
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Text("$pendingCount", style = MaterialTheme.typography.titleMedium)
                    }
                }
            }
            if (onRenameClick != null || onArchiveClick != null) {
                var showMenu by remember { mutableStateOf(false) }
                Box {
                    IconButton(onClick = { showMenu = true }) {
                        Icon(Icons.Default.MoreVert, contentDescription = stringResource(Res.string.options))
                    }
                    DropdownMenu(expanded = showMenu, onDismissRequest = { showMenu = false }) {
                        if (onRenameClick != null) {
                            DropdownMenuItem(
                                text = { Text(stringResource(Res.string.rename)) },
                                onClick = { showMenu = false; onRenameClick() },
                            )
                        }
                        if (onArchiveClick != null) {
                            DropdownMenuItem(
                                text = { Text(stringResource(Res.string.archive)) },
                                onClick = { showMenu = false; onArchiveClick() },
                            )
                        }
                    }
                }
            } else {
                Spacer(modifier = Modifier.width(4.dp))
            }
        }
    }
}
