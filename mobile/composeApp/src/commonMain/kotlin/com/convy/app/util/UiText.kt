package com.convy.app.util

import androidx.compose.runtime.Composable
import org.jetbrains.compose.resources.StringResource
import org.jetbrains.compose.resources.stringResource

sealed interface UiText {
    data class DynamicString(val value: String) : UiText
    data class StringResourceText(val res: StringResource, val args: List<Any> = emptyList()) : UiText

    @Composable
    fun asString(): String = when (this) {
        is DynamicString -> value
        is StringResourceText -> if (args.isEmpty()) {
            stringResource(res)
        } else {
            stringResource(res, *args.toTypedArray())
        }
    }

    companion object {
        fun fromError(message: String?, fallback: StringResource): UiText =
            message?.let { DynamicString(it) } ?: StringResourceText(fallback)
    }
}
