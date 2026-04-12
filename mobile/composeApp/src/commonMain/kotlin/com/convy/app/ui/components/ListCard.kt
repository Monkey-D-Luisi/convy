package com.convy.app.ui.components

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.convy.shared.domain.model.HouseholdList
import com.convy.shared.domain.model.ListType
import com.convy.app.generated.resources.*
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
    Card(
        onClick = onClick,
        modifier = modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surfaceContainerLowest,
        ),
    ) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Icon(
                imageVector = when (list.type) {
                    ListType.Shopping -> Icons.Default.ShoppingCart
                    ListType.Tasks -> Icons.Default.CheckCircle
                },
                contentDescription = null,
                tint = MaterialTheme.colorScheme.primary,
                modifier = Modifier.size(28.dp),
            )
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = list.name,
                    style = MaterialTheme.typography.titleMedium,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
                Spacer(modifier = Modifier.height(4.dp))
                AssistChip(
                    onClick = {},
                    label = {
                        Text(
                            text = when (list.type) {
                                ListType.Shopping -> stringResource(Res.string.lists_type_shopping)
                                ListType.Tasks -> stringResource(Res.string.lists_type_tasks)
                            },
                            style = MaterialTheme.typography.labelSmall,
                        )
                    },
                    colors = AssistChipDefaults.assistChipColors(
                        containerColor = MaterialTheme.colorScheme.secondaryContainer,
                        labelColor = MaterialTheme.colorScheme.onSecondaryContainer,
                    ),
                )
            }
            if (pendingCount > 0) {
                Badge(
                    containerColor = MaterialTheme.colorScheme.primary,
                    contentColor = MaterialTheme.colorScheme.onPrimary,
                ) {
                    Text("$pendingCount")
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
            }
        }
    }
}
