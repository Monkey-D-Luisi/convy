package com.convy

import com.convy.shared.data.remote.PushTokenProvider
import com.google.firebase.messaging.FirebaseMessaging
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

class AndroidPushTokenProvider : PushTokenProvider {
    override suspend fun getToken(): String? {
        return try {
            suspendCancellableCoroutine { cont ->
                FirebaseMessaging.getInstance().token
                    .addOnSuccessListener { token -> cont.resume(token) }
                    .addOnFailureListener { e -> cont.resumeWithException(e) }
            }
        } catch (_: Exception) {
            null
        }
    }
}
