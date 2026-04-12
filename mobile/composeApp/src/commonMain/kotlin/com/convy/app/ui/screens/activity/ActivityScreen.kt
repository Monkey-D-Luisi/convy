package com.convy.app.ui.screens.activity

import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.convy.app.ui.components.EmptyContent
import com.convy.app.ui.components.ErrorContent
import com.convy.app.ui.components.LoadingContent
import com.convy.shared.domain.model.ActivityLogEntry
import com.convy.app.generated.resources.*
import org.jetbrains.compose.resources.stringResource

@Composable
fun ActivityScreen(
    store: ActivityStore,
    onNavigateBack: () -> Unit,
) {
    val state by store.state.collectAsState()

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is ActivitySideEffect.NavigateBack -> onNavigateBack()
                is ActivitySideEffect.ShowError -> {}
            }
        }
    }

    ActivityContent(state = state, onIntent = store::processIntent)
}

@OptIn(ExperimentalMaterial3Api::class, ExperimentalFoundationApi::class)
@Composable
fun ActivityContent(
    state: ActivityState,
    onIntent: (ActivityIntent) -> Unit,
) {
    Scaffold(
        topBar = {
            TopAppBar(
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
    ) { padding ->
        when {
            state.isLoading -> LoadingContent(modifier = Modifier.padding(padding))
            state.error != null -> ErrorContent(
                message = state.error.asString(),
                onRetry = { onIntent(ActivityIntent.Refresh) },
                modifier = Modifier.padding(padding),
            )
            state.groupedEntries.isEmpty() -> EmptyContent(
                message = stringResource(Res.string.activity_empty),
                modifier = Modifier.padding(padding),
            )
            else -> LazyColumn(
                contentPadding = PaddingValues(
                    start = 16.dp, end = 16.dp,
                    top = padding.calculateTopPadding() + 16.dp,
                    bottom = padding.calculateBottomPadding() + 16.dp,
                ),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                state.groupedEntries.forEach { group ->
                    stickyHeader {
                        Surface(
                            modifier = Modifier.fillMaxWidth(),
                            color = MaterialTheme.colorScheme.surface,
                        ) {
                            Text(
                                text = if (group.date == "Today") stringResource(Res.string.activity_today) else group.date,
                                style = MaterialTheme.typography.titleSmall,
                                fontWeight = FontWeight.Bold,
                                color = MaterialTheme.colorScheme.primary,
                                modifier = Modifier.padding(vertical = 8.dp),
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

@Composable
private fun ActivityEntryCard(entry: ActivityLogEntry) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surfaceContainerLowest,
        ),
    ) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Surface(
                modifier = Modifier.size(40.dp),
                shape = MaterialTheme.shapes.extraLarge,
                color = MaterialTheme.colorScheme.secondaryContainer,
            ) {
                Box(contentAlignment = Alignment.Center) {
                    Text(
                        text = actionIcon(entry.actionType),
                        style = MaterialTheme.typography.titleMedium,
                    )
                }
            }
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = formatAction(entry),
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.Medium,
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

private fun actionIcon(actionType: String): String = when (actionType) {
    "Created" -> "+"
    "Updated" -> "✎"
    "Completed" -> "✓"
    "Uncompleted" -> "↩"
    "Deleted" -> "✕"
    "Archived" -> "📦"
    "Renamed" -> "✎"
    "MemberJoined" -> "👤"
    else -> "•"
}

@Composable
private fun formatAction(entry: ActivityLogEntry): String {
    val entityLabel = entry.entityType.lowercase()
    val metadata = entry.metadata?.let { " \"$it\"" } ?: ""
    return when (entry.actionType) {
        "Created" -> stringResource(Res.string.activity_created, entityLabel, metadata)
        "Updated" -> stringResource(Res.string.activity_updated, entityLabel, metadata)
        "Completed" -> stringResource(Res.string.activity_completed, entityLabel, metadata)
        "Uncompleted" -> stringResource(Res.string.activity_uncompleted, entityLabel, metadata)
        "Deleted" -> stringResource(Res.string.activity_deleted, entityLabel)
        "Archived" -> stringResource(Res.string.activity_archived, entityLabel, metadata)
        "Renamed" -> stringResource(Res.string.activity_renamed, entityLabel, metadata)
        "MemberJoined" -> stringResource(Res.string.activity_member_joined)
        else -> stringResource(Res.string.activity_default, entityLabel, entry.actionType.lowercase())
    }
}
