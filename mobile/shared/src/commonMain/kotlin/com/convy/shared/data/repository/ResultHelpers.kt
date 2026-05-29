package com.convy.shared.data.repository

import com.convy.shared.data.remote.toApiResponseException
import io.ktor.client.plugins.ResponseException
import kotlin.coroutines.cancellation.CancellationException

internal suspend fun <T> cancellableRunCatching(block: suspend () -> T): Result<T> =
    try {
        Result.success(block())
    } catch (e: CancellationException) {
        throw e
    } catch (e: ResponseException) {
        Result.failure(e.toApiResponseException())
    } catch (e: Exception) {
        Result.failure(e)
    }
