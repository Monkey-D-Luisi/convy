package com.convy.shared.domain.model

data class ParsedItem(
    val title: String,
    val quantity: Int?,
    val unit: String?,
    val matchedExistingItem: String? = null,
)

data class VoiceParseResult(
    val transcription: String,
    val items: List<ParsedItem>,
)
