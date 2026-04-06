package com.convy

import android.content.Context
import android.media.MediaRecorder
import android.os.Build
import com.convy.shared.platform.AudioRecorder
import java.io.File

class AndroidAudioRecorder(
    private val context: Context,
) : AudioRecorder {

    private var recorder: MediaRecorder? = null
    private var outputFile: File? = null
    private var recording = false

    override fun startRecording() {
        val file = File(context.cacheDir, "voice_recording.m4a")
        outputFile = file

        recorder = createMediaRecorder().apply {
            setAudioSource(MediaRecorder.AudioSource.MIC)
            setOutputFormat(MediaRecorder.OutputFormat.MPEG_4)
            setAudioEncoder(MediaRecorder.AudioEncoder.AAC)
            setAudioSamplingRate(44100)
            setAudioEncodingBitRate(128000)
            setMaxDuration(30_000)
            setOutputFile(file.absolutePath)
            prepare()
            start()
        }
        recording = true
    }

    override fun stopRecording(): ByteArray? {
        return try {
            recorder?.stop()
            recorder?.release()
            recorder = null
            recording = false
            outputFile?.readBytes()
        } catch (e: Exception) {
            recorder?.release()
            recorder = null
            recording = false
            null
        } finally {
            outputFile?.delete()
            outputFile = null
        }
    }

    override fun isRecording(): Boolean = recording

    override fun release() {
        recorder?.release()
        recorder = null
        recording = false
        outputFile?.delete()
        outputFile = null
    }

    @Suppress("DEPRECATION")
    private fun createMediaRecorder(): MediaRecorder {
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            MediaRecorder(context)
        } else {
            MediaRecorder()
        }
    }
}
