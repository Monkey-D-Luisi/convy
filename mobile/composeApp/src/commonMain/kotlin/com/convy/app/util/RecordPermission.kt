package com.convy.app.util

import androidx.compose.runtime.Composable

class RecordAudioPermissionState(
    val isGranted: Boolean,
    val launchRequest: () -> Unit,
)

@Composable
expect fun rememberRecordAudioPermissionState(): RecordAudioPermissionState
