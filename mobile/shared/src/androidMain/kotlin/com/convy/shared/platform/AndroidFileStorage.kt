package com.convy.shared.platform

import android.content.Context
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.File

class AndroidFileStorage(private val context: Context) : FileStorage {

    override suspend fun read(filename: String): String? = withContext(Dispatchers.IO) {
        val file = File(context.filesDir, filename)
        if (file.exists()) file.readText() else null
    }

    override suspend fun write(filename: String, content: String) = withContext(Dispatchers.IO) {
        val file = File(context.filesDir, filename)
        file.writeText(content)
    }
}
