package com.convy.app.mvi

import kotlinx.coroutines.flow.StateFlow

interface MviStore<S : Any, I : Any, E : Any> {
    val state: StateFlow<S>
    fun dispatch(intent: I)
}
