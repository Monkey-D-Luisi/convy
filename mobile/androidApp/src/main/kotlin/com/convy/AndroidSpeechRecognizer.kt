package com.convy

import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.speech.RecognitionListener
import android.speech.RecognizerIntent
import android.speech.SpeechRecognizer as AndroidSR
import com.convy.shared.platform.SpeechRecognizer

class AndroidSpeechRecognizer(
    private val context: Context,
) : SpeechRecognizer {

    private var recognizer: AndroidSR? = null

    override fun startListening(onResult: (String) -> Unit, onError: (String) -> Unit) {
        if (!isAvailable()) {
            onError("Speech recognition not available")
            return
        }

        recognizer = AndroidSR.createSpeechRecognizer(context).apply {
            setRecognitionListener(object : RecognitionListener {
                override fun onReadyForSpeech(params: Bundle?) {}
                override fun onBeginningOfSpeech() {}
                override fun onRmsChanged(rmsdB: Float) {}
                override fun onBufferReceived(buffer: ByteArray?) {}
                override fun onEndOfSpeech() {}
                override fun onError(error: Int) {
                    val msg = when (error) {
                        AndroidSR.ERROR_NO_MATCH -> "No speech detected"
                        AndroidSR.ERROR_SPEECH_TIMEOUT -> "Speech timeout"
                        AndroidSR.ERROR_AUDIO -> "Audio error"
                        else -> "Recognition error ($error)"
                    }
                    onError(msg)
                }
                override fun onResults(results: Bundle?) {
                    val matches = results?.getStringArrayList(AndroidSR.RESULTS_RECOGNITION)
                    val text = matches?.firstOrNull() ?: ""
                    onResult(text)
                }
                override fun onPartialResults(partialResults: Bundle?) {}
                override fun onEvent(eventType: Int, params: Bundle?) {}
            })
        }

        val intent = Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH).apply {
            putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM)
            putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 1)
        }

        recognizer?.startListening(intent)
    }

    override fun stopListening() {
        recognizer?.stopListening()
        recognizer?.destroy()
        recognizer = null
    }

    override fun isAvailable(): Boolean =
        AndroidSR.isRecognitionAvailable(context)
}
