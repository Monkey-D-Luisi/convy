package com.convy.shared.data.repository

import kotlin.coroutines.cancellation.CancellationException

internal suspend fun <T> cancellableRunCatching(block: suspend () -> T): Result<T> =
    try {
        Result.success(block())
    } catch (e: CancellationException) {
        throw e
    } catch (e: Exception) {
        Result.failure(e)
    }
