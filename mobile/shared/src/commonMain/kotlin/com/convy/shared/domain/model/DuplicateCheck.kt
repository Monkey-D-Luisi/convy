package com.convy.shared.domain.model

data class DuplicateCheck(
    val hasPotentialDuplicates: Boolean,
    val potentialDuplicates: List<DuplicateItem>,
)

data class DuplicateItem(
    val id: String,
    val title: String,
    val quantity: Int?,
    val unit: String?,
)
