package com.convy.app.ui.mvi

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel

abstract class MviStore {
    protected val scope = CoroutineScope(Dispatchers.Main + SupervisorJob())

    open fun close() {
        scope.cancel()
    }
}
