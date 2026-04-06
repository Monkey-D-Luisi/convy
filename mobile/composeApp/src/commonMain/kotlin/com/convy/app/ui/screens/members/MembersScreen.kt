package com.convy.app.ui.screens.members

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Close
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalClipboardManager
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.convy.app.ui.components.ErrorContent
import com.convy.app.ui.components.LoadingContent
import com.convy.shared.domain.model.HouseholdMember
import com.convy.shared.domain.model.HouseholdRole

@Composable
fun MembersScreen(
    store: MembersStore,
    onNavigateBack: () -> Unit,
) {
    val state by store.state.collectAsState()
    val clipboardManager = LocalClipboardManager.current

    LaunchedEffect(Unit) {
        store.sideEffects.collect { effect ->
            when (effect) {
                is MembersSideEffect.NavigateBack -> onNavigateBack()
                is MembersSideEffect.ShareInviteCode -> {
                    clipboardManager.setText(AnnotatedString(effect.code))
                }
                is MembersSideEffect.ShowError -> {}
            }
        }
    }

    MembersContent(state = state, onIntent = store::processIntent)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MembersContent(
    state: MembersState,
    onIntent: (MembersIntent) -> Unit,
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Members") },
                navigationIcon = {
                    IconButton(
                        onClick = { onIntent(MembersIntent.NavigateBack) },
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
                onRetry = { onIntent(MembersIntent.Refresh) },
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
                items(state.members, key = { it.userId }) { member ->
                    MemberCard(member)
                }

                item {
                    Spacer(modifier = Modifier.height(16.dp))
                    InviteSection(
                        invite = state.invite,
                        isGenerating = state.isGeneratingInvite,
                        onGenerateInvite = { onIntent(MembersIntent.GenerateInvite) },
                        onCopyCode = { state.invite?.let { onIntent(MembersIntent.CopyInviteCode) } },
                    )
                }

                if (state.activeInvites.isNotEmpty()) {
                    item {
                        Spacer(modifier = Modifier.height(16.dp))
                        Text(
                            "Active invites",
                            style = MaterialTheme.typography.titleMedium,
                            modifier = Modifier.padding(horizontal = 4.dp),
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                    }
                    items(state.activeInvites, key = { it.id }) { invite ->
                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            colors = CardDefaults.cardColors(
                                containerColor = MaterialTheme.colorScheme.surfaceContainerLowest,
                            ),
                        ) {
                            Row(
                                modifier = Modifier.padding(16.dp).fillMaxWidth(),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically,
                            ) {
                                Column {
                                    Text(
                                        text = invite.code,
                                        style = MaterialTheme.typography.titleSmall,
                                        fontWeight = FontWeight.Bold,
                                    )
                                    Text(
                                        text = "Expires: ${invite.expiresAt}",
                                        style = MaterialTheme.typography.bodySmall,
                                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                                    )
                                }
                                IconButton(
                                    onClick = { onIntent(MembersIntent.RevokeInvite(invite.id)) },
                                    modifier = Modifier.testTag("Revoke invite"),
                                ) {
                                    Icon(
                                        Icons.Default.Close,
                                        contentDescription = "Revoke",
                                        tint = MaterialTheme.colorScheme.error,
                                    )
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
private fun MemberCard(member: HouseholdMember) {
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
            // Avatar with initial
            Surface(
                modifier = Modifier.size(40.dp),
                shape = MaterialTheme.shapes.extraLarge,
                color = MaterialTheme.colorScheme.primaryContainer,
            ) {
                Box(contentAlignment = Alignment.Center) {
                    Text(
                        text = member.displayName.firstOrNull()?.uppercase() ?: "?",
                        style = MaterialTheme.typography.titleMedium,
                        color = MaterialTheme.colorScheme.onPrimaryContainer,
                    )
                }
            }
            Spacer(modifier = Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = member.displayName,
                    style = MaterialTheme.typography.titleMedium,
                )
                Text(
                    text = member.email,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            if (member.role == HouseholdRole.Owner) {
                AssistChip(
                    onClick = {},
                    label = { Text("Owner") },
                )
            }
        }
    }
}

@Composable
private fun InviteSection(
    invite: com.convy.shared.domain.model.Invite?,
    isGenerating: Boolean,
    onGenerateInvite: () -> Unit,
    onCopyCode: () -> Unit,
) {
    Card(
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.secondaryContainer,
        ),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(
                text = "Invite someone",
                style = MaterialTheme.typography.titleMedium,
            )
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = "Generate an invite code to share with someone you want to add to this household.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSecondaryContainer,
            )

            if (invite != null) {
                Spacer(modifier = Modifier.height(12.dp))
                Card(
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.surface,
                    ),
                ) {
                    Column(modifier = Modifier.padding(12.dp)) {
                        Text(
                            text = "Invite code:",
                            style = MaterialTheme.typography.labelMedium,
                        )
                        Text(
                            text = invite.code,
                            style = MaterialTheme.typography.headlineSmall,
                            fontWeight = FontWeight.Bold,
                        )
                    }
                }
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedButton(
                    onClick = onCopyCode,
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    Text("Copy code")
                }
            }

            Spacer(modifier = Modifier.height(12.dp))
            Button(
                onClick = onGenerateInvite,
                enabled = !isGenerating,
                modifier = Modifier.testTag("Generate invite code"),
            ) {
                if (isGenerating) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(16.dp),
                        strokeWidth = 2.dp,
                    )
                    Spacer(modifier = Modifier.width(8.dp))
                }
                Icon(Icons.Default.Add, contentDescription = null)
                Spacer(modifier = Modifier.width(8.dp))
                Text(if (invite != null) "Generate new code" else "Generate invite code")
            }
        }
    }
}
