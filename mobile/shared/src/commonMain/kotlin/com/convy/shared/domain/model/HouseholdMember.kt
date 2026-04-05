package com.convy.shared.domain.model

data class HouseholdMember(
    val userId: String,
    val displayName: String,
    val email: String,
    val role: HouseholdRole,
    val joinedAt: String,
)

enum class HouseholdRole {
    Owner,
    Member,
}
