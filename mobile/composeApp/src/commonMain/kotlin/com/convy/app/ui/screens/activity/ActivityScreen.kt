package com.convy.app.ui.screens.activity

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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ActivityContent(
    state: ActivityState,
    onIntent: (ActivityIntent) -> Unit,
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Activity") },
                navigationIcon = {
                    IconButton(
                        onClick = { onIntent(ActivityIntent.NavigateBack) },
                        modifier = Modifier.testTag("Back"),
                    ) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
            )
        },
    ) { padding ->
        when {
            state.isLoading -> LoadingContent(modifier = Modifier.padding(padding))
            state.error != null -> ErrorContent(
                message = state.error,
                onRetry = { onIntent(ActivityIntent.Refresh) },
                modifier = Modifier.padding(padding),
            )
            state.entries.isEmpty() -> EmptyContent(
                message = "No activity yet",
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
                items(state.entries, key = { it.id }) { entry ->
                    ActivityEntryCard(entry)
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

private fun formatAction(entry: ActivityLogEntry): String {
    val entityLabel = entry.entityType.lowercase()
    val metadata = entry.metadata?.let { " \"$it\"" } ?: ""
    return when (entry.actionType) {
        "Created" -> "Created $entityLabel$metadata"
        "Updated" -> "Updated $entityLabel$metadata"
        "Completed" -> "Completed $entityLabel$metadata"
        "Uncompleted" -> "Marked $entityLabel$metadata as pending"
        "Deleted" -> "Deleted $entityLabel"
        "Archived" -> "Archived $entityLabel$metadata"
        "Renamed" -> "Renamed $entityLabel to$metadata"
        "MemberJoined" -> "Joined the household"
        else -> "$entityLabel ${entry.actionType.lowercase()}"
    }
}
