package com.convy.shared.platform

import kotlinx.coroutines.flow.StateFlow

interface NetworkMonitor {
    val isOnline: StateFlow<Boolean>
    fun isCurrentlyOnline(): Boolean
}
