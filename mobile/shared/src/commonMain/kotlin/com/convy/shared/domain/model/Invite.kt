package com.convy.shared.domain.model

data class Invite(
    val id: String,
    val householdId: String,
    val code: String,
    val expiresAt: String,
    val isValid: Boolean,
    val createdAt: String,
)
