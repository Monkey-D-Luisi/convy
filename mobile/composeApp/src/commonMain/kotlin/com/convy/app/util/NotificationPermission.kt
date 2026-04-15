package com.convy.app.util

import androidx.compose.runtime.Composable

class NotificationPermissionState(
    val isGranted: Boolean,
    val launchRequest: () -> Unit,
)

@Composable
expect fun rememberNotificationPermissionState(): NotificationPermissionState
