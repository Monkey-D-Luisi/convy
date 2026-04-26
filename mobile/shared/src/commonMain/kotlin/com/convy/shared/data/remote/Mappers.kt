package com.convy.shared.data.remote

import com.convy.shared.data.remote.dto.*
import com.convy.shared.domain.model.*

fun UserDto.toDomain(): User = User(
    id = id,
    displayName = displayName,
    email = email,
    createdAt = createdAt,
)

fun HouseholdDto.toDomain(): Household = Household(
    id = id,
    name = name,
    createdBy = createdBy,
    createdAt = createdAt,
)

fun HouseholdDetailDto.toDomain(): HouseholdDetail = HouseholdDetail(
    id = id,
    name = name,
    createdBy = createdBy,
    createdAt = createdAt,
    members = members.map { it.toDomain() },
)

fun HouseholdMemberDto.toDomain(): HouseholdMember = HouseholdMember(
    userId = userId,
    displayName = displayName,
    email = email,
    role = when (role) {
        "Owner" -> HouseholdRole.Owner
        else -> HouseholdRole.Member
    },
    joinedAt = joinedAt,
)

fun HouseholdListDto.toDomain(): HouseholdList = HouseholdList(
    id = id,
    name = name,
    type = when (type) {
        "Shopping" -> ListType.Shopping
        else -> ListType.Tasks
    },
    householdId = householdId,
    createdBy = createdBy,
    createdAt = createdAt,
    isArchived = isArchived,
    archivedAt = archivedAt,
)

fun ListItemDto.toDomain(): ListItem = ListItem(
    id = id,
    title = title,
    quantity = quantity,
    unit = unit,
    note = note,
    listId = listId,
    createdBy = createdBy,
    createdByName = createdByName,
    createdAt = createdAt,
    isCompleted = isCompleted,
    completedBy = completedBy,
    completedByName = completedByName,
    completedAt = completedAt,
    recurrenceFrequency = recurrenceFrequency,
    recurrenceInterval = recurrenceInterval,
    nextDueDate = nextDueDate,
)

fun TaskItemDto.toDomain(): TaskItem = TaskItem(
    id = id,
    title = title,
    note = note,
    listId = listId,
    createdBy = createdBy,
    createdByName = createdByName,
    createdAt = createdAt,
    isCompleted = isCompleted,
    completedBy = completedBy,
    completedByName = completedByName,
    completedAt = completedAt,
)

fun InviteDto.toDomain(): Invite = Invite(
    id = id,
    householdId = householdId,
    code = code,
    expiresAt = expiresAt,
    isValid = isValid,
    createdAt = createdAt,
)

fun DuplicateCheckResponseDto.toDomain(): DuplicateCheck = DuplicateCheck(
    hasPotentialDuplicates = hasPotentialDuplicates,
    potentialDuplicates = potentialDuplicates.map { it.toDomain() },
)

fun DuplicateItemDto.toDomain(): DuplicateItem = DuplicateItem(
    id = id,
    title = title,
    quantity = quantity,
    unit = unit,
)

fun ActivityLogEntryDto.toDomain(): ActivityLogEntry = ActivityLogEntry(
    id = id,
    householdId = householdId,
    entityType = entityType,
    entityId = entityId,
    actionType = actionType,
    performedBy = performedBy,
    performedByName = performedByName,
    createdAt = createdAt,
    metadata = metadata,
)
