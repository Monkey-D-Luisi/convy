package com.convy.shared.data.remote

import com.convy.shared.data.remote.dto.ErrorResponse
import io.ktor.client.plugins.ResponseException
import io.ktor.client.statement.HttpResponse
import io.ktor.client.statement.bodyAsText
import kotlinx.serialization.decodeFromString
import kotlinx.serialization.json.Json

private const val MaxApiErrorMessageLength = 256

internal class ApiResponseException(
    val statusCode: Int,
    val errorCode: String?,
    message: String,
    cause: Throwable? = null,
) : Exception(message, cause)

internal suspend fun HttpResponse.toApiResponseException(json: Json): ApiResponseException =
    toApiResponseException(json, cause = null)

internal suspend fun ResponseException.toApiResponseException(): ApiResponseException =
    response.toApiResponseException(ApiErrorJson, cause = this)

private suspend fun HttpResponse.toApiResponseException(json: Json, cause: Throwable?): ApiResponseException {
    val statusCode = status.value
    val error = runCatching {
        json.decodeFromString<ErrorResponse>(bodyAsText())
    }.getOrNull()
    val message = error?.message
        ?.trim()
        ?.takeIf { it.isNotEmpty() }
        ?.take(MaxApiErrorMessageLength)
        ?: "Convy request failed with HTTP $statusCode."

    return ApiResponseException(statusCode, error?.code, message, cause)
}

private val ApiErrorJson = Json {
    ignoreUnknownKeys = true
}
