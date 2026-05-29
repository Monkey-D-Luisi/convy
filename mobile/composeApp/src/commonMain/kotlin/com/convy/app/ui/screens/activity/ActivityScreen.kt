package com.convy.app.ui.screens.activity

import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.Undo
import androidx.compose.material.icons.filled.AddCircleOutline
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.DriveFileRenameOutline
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.Inventory2
import androidx.compose.material.icons.filled.PersonAdd
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.convy.app.generated.resources.Res
import com.convy.app.generated.resources.activity_empty
import com.convy.app.generated.resources.activity_load_more
import com.convy.app.generated.resources.activity_title
import com.convy.app.generated.resources.activity_today
import com.convy.app.generated.resources.back
import com.convy.app.ui.components.ConvyBackground
import com.convy.app.ui.components.ConvyIconBubble
import com.convy.app.ui.components.ConvySpacing
import com.convy.app.ui.components.EmptyContent
import com.convy.app.ui.components.ErrorContent
import com.convy.app.ui.components.LoadingContent
import com.convy.app.ui.components.convyTopAppBarColors
import com.convy.shared.domain.model.ActivityLogEntry
import org.jetbrains.compose.resources.stringResource

@Composable
fun ActivityScreen(
    store: ActivityStore,
    onNavigateBack: () -> Unit,
) {
    val state by store.state.collectAsState()
    val snackbarHostState = remember { SnackbarHostState() }

    DisposableEffect(store) {
        onDispose { store.close() }
    }

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is ActivitySideEffect.NavigateBack -> onNavigateBack()
                is ActivitySideEffect.ShowError -> snackbarHostState.showSnackbar(effect.message)
            }
        }
    }

    ActivityContent(
        state = state,
        onIntent = store::processIntent,
        snackbarHostState = snackbarHostState,
    )
}

@OptIn(ExperimentalMaterial3Api::class, ExperimentalFoundationApi::class)
@Composable
fun ActivityContent(
    state: ActivityState,
    onIntent: (ActivityIntent) -> Unit,
    snackbarHostState: SnackbarHostState = remember { SnackbarHostState() },
) {
    Scaffold(
        topBar = {
            TopAppBar(
                colors = convyTopAppBarColors(),
                title = { Text(stringResource(Res.string.activity_title)) },
                navigationIcon = {
                    IconButton(
                        onClick = { onIntent(ActivityIntent.NavigateBack) },
                        modifier = Modifier.testTag("Back"),
                    ) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = stringResource(Res.string.back))
                    }
                },
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) },
    ) { padding ->
        ConvyBackground(modifier = Modifier.padding(padding)) {
            when {
                state.isLoading -> LoadingContent()
                state.error != null -> ErrorContent(
                    message = state.error.asString(),
                    onRetry = { onIntent(ActivityIntent.Refresh) },
                )
                state.groupedEntries.isEmpty() -> EmptyContent(
                    message = stringResource(Res.string.activity_empty),
                )
                else -> LazyColumn(
                    contentPadding = PaddingValues(
                        start = ConvySpacing.ScreenHorizontal,
                        end = ConvySpacing.ScreenHorizontal,
                        top = ConvySpacing.ScreenTop,
                        bottom = 24.dp,
                    ),
                    verticalArrangement = Arrangement.spacedBy(10.dp),
                ) {
                    state.groupedEntries.forEach { group ->
                        stickyHeader {
                            Surface(
                                modifier = Modifier.fillMaxWidth(),
                                color = MaterialTheme.colorScheme.surface.copy(alpha = 0.92f),
                                shape = MaterialTheme.shapes.large,
                            ) {
                                Text(
                                    text = if (group.date == "Today") stringResource(Res.string.activity_today) else group.date,
                                    style = MaterialTheme.typography.titleSmall,
                                    fontWeight = FontWeight.Bold,
                                    color = MaterialTheme.colorScheme.primary,
                                    modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp),
                                )
                            }
                        }
                        items(group.entries, key = { it.id }) { entry ->
                            ActivityEntryCard(entry)
                        }
                    }
                    if (state.hasMore) {
                        item {
                            Box(
                                modifier = Modifier.fillMaxWidth().padding(16.dp),
                                contentAlignment = Alignment.Center,
                            ) {
                                if (state.isLoadingMore) {
                                    CircularProgressIndicator(modifier = Modifier.size(24.dp))
                                } else {
                                    TextButton(onClick = { onIntent(ActivityIntent.LoadMore) }) {
                                        Text(stringResource(Res.string.activity_load_more))
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun ActivityEntryCard(entry: ActivityLogEntry) {
    val tint = actionTint(entry.actionType)
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = MaterialTheme.shapes.large,
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surface.copy(alpha = 0.96f),
        ),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp),
    ) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            ConvyIconBubble(
                icon = actionIcon(entry.actionType),
                contentDescription = null,
                size = 46.dp,
                tint = tint,
                containerColor = tint.copy(alpha = 0.12f),
            )
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = formatAction(entry),
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.SemiBold,
                )
                Text(
                    text = entry.performedByName,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }
    }
}

private fun actionIcon(actionType: String): ImageVector = when (actionType) {
    "Created" -> Icons.Default.AddCircleOutline
    "Updated" -> Icons.Default.Edit
    "Completed" -> Icons.Default.CheckCircle
    "Uncompleted" -> Icons.AutoMirrored.Filled.Undo
    "Deleted" -> Icons.Default.Delete
    "Archived" -> Icons.Default.Inventory2
    "Renamed" -> Icons.Default.DriveFileRenameOutline
    "MemberJoined" -> Icons.Default.PersonAdd
    else -> Icons.Default.Edit
}

@Composable
private fun actionTint(actionType: String): Color = when (actionType) {
    "Deleted" -> MaterialTheme.colorScheme.error
    "Renamed", "Updated" -> MaterialTheme.colorScheme.secondary
    else -> MaterialTheme.colorScheme.primary
}

@Composable
private fun formatAction(entry: ActivityLogEntry): String {
    val text = activityActionText(entry)
    return stringResource(text.resource, *text.args.toTypedArray())
}
