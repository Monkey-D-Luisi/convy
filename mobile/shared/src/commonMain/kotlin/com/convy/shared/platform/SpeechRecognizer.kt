package com.convy.shared.platform

interface SpeechRecognizer {
    fun startListening(onResult: (String) -> Unit, onError: (String) -> Unit)
    fun stopListening()
    fun isAvailable(): Boolean
}
