package com.convy.app.ui.screens.startup

import com.convy.shared.data.remote.DeviceTokenManager
import com.convy.shared.domain.repository.ActiveHouseholdRepository
import com.convy.shared.domain.repository.AuthRepository
import com.convy.shared.domain.repository.HouseholdRepository
import com.convy.shared.domain.repository.UserRepository
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.launch

class StartupStore(
    private val authRepository: AuthRepository,
    private val userRepository: UserRepository,
    private val householdRepository: HouseholdRepository,
    private val deviceTokenManager: DeviceTokenManager,
    private val activeHouseholdRepository: ActiveHouseholdRepository,
) {
    suspend fun resolveDestination(): StartupDestination = coroutineScope {
        val user = authRepository.getCurrentUser() ?: return@coroutineScope StartupDestination.Auth

        userRepository.register(user.id, user.displayName, user.email)
        launch { deviceTokenManager.registerCurrentToken() }

        val households = householdRepository.getMyHouseholds().getOrNull().orEmpty()
        val activeHousehold = activeHouseholdRepository.resolveActiveHousehold(households)

        if (activeHousehold == null) {
            StartupDestination.HouseholdSetup
        } else {
            StartupDestination.HouseholdLists(activeHousehold.id)
        }
    }
}

sealed interface StartupDestination {
    data object Auth : StartupDestination
    data object HouseholdSetup : StartupDestination
    data class HouseholdLists(val householdId: String) : StartupDestination
}
