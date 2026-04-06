package com.convy.shared.platform

interface AudioRecorder {
    fun startRecording()
    fun stopRecording(): ByteArray?
    fun isRecording(): Boolean
    fun release()
}
