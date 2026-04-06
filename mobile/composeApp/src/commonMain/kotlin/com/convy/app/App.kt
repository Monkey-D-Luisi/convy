package com.convy.app

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import com.convy.app.navigation.AppNavigator
import com.convy.app.navigation.NavRoute
import com.convy.app.ui.screens.activity.ActivityScreen
import com.convy.app.ui.screens.activity.ActivityStore
import com.convy.app.ui.screens.auth.AuthScreen
import com.convy.app.ui.screens.auth.AuthStore
import com.convy.app.ui.screens.householdsetup.HouseholdSetupScreen
import com.convy.app.ui.screens.householdsetup.HouseholdSetupStore
import com.convy.app.ui.screens.item.ItemFormScreen
import com.convy.app.ui.screens.item.ItemFormStore
import com.convy.app.ui.screens.listdetail.ListDetailScreen
import com.convy.app.ui.screens.listdetail.ListDetailStore
import com.convy.app.ui.screens.lists.HouseholdListsScreen
import com.convy.app.ui.screens.lists.HouseholdListsStore
import com.convy.app.ui.screens.members.MembersScreen
import com.convy.app.ui.screens.members.MembersStore
import com.convy.app.ui.screens.settings.SettingsScreen
import com.convy.app.ui.screens.settings.SettingsStore
import com.convy.app.ui.theme.ConvyTheme
import com.convy.shared.domain.repository.AuthRepository
import com.convy.shared.domain.repository.HouseholdRepository
import org.koin.compose.koinInject
import org.koin.core.parameter.parametersOf

@Composable
fun App() {
    ConvyTheme {
        val navigator = koinInject<AppNavigator>()
        val authRepository = koinInject<AuthRepository>()
        val householdRepository = koinInject<HouseholdRepository>()
        val currentRoute by navigator.currentRoute.collectAsState()
        var isCheckingAuth by remember { mutableStateOf(true) }

        LaunchedEffect(Unit) {
            val user = authRepository.getCurrentUser()
            if (user != null) {
                val households = householdRepository.getMyHouseholds().getOrNull()
                if (!households.isNullOrEmpty()) {
                    navigator.replaceWith(NavRoute.HouseholdLists(households.first().id))
                } else {
                    navigator.replaceWith(NavRoute.HouseholdSetup)
                }
            }
            isCheckingAuth = false
        }

        if (isCheckingAuth) {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center,
            ) {
                CircularProgressIndicator()
            }
            return@ConvyTheme
        }

        when (val route = currentRoute) {
            is NavRoute.Auth -> {
                val store = koinInject<AuthStore>()
                AuthScreen(
                    store = store,
                    onNavigateToHouseholdSetup = {
                        navigator.replaceWith(NavRoute.HouseholdSetup)
                    },
                    onNavigateToLists = { householdId ->
                        navigator.replaceWith(NavRoute.HouseholdLists(householdId))
                    },
                )
            }

            is NavRoute.HouseholdSetup -> {
                val store = koinInject<HouseholdSetupStore>()
                HouseholdSetupScreen(
                    store = store,
                    onNavigateToLists = { householdId ->
                        navigator.replaceWith(NavRoute.HouseholdLists(householdId))
                    },
                )
            }

            is NavRoute.HouseholdLists -> {
                val store = koinInject<HouseholdListsStore> { parametersOf(route.householdId) }
                HouseholdListsScreen(
                    store = store,
                    onNavigateToList = { householdId, listId, listName ->
                        navigator.navigateTo(NavRoute.ListDetail(householdId, listId, listName))
                    },
                    onNavigateToMembers = { householdId ->
                        navigator.navigateTo(NavRoute.Members(householdId))
                    },
                    onNavigateToActivity = { householdId ->
                        navigator.navigateTo(NavRoute.Activity(householdId))
                    },
                    onNavigateToSettings = {
                        navigator.navigateTo(NavRoute.Settings)
                    },
                )
            }

            is NavRoute.ListDetail -> {
                val store = koinInject<ListDetailStore> {
                    parametersOf(route.householdId, route.listId, route.listName)
                }
                ListDetailScreen(
                    store = store,
                    onNavigateToCreateItem = { householdId, listId ->
                        navigator.navigateTo(NavRoute.CreateItem(householdId, listId))
                    },
                    onNavigateToEditItem = { householdId, listId, itemId ->
                        navigator.navigateTo(NavRoute.EditItem(householdId, listId, itemId))
                    },
                    onNavigateBack = { navigator.navigateBack() },
                )
            }

            is NavRoute.CreateItem -> {
                val store = koinInject<ItemFormStore> {
                    parametersOf(route.householdId, route.listId, null)
                }
                ItemFormScreen(
                    store = store,
                    onNavigateBack = { navigator.navigateBack() },
                )
            }

            is NavRoute.EditItem -> {
                val store = koinInject<ItemFormStore> {
                    parametersOf(route.householdId, route.listId, route.itemId)
                }
                ItemFormScreen(
                    store = store,
                    onNavigateBack = { navigator.navigateBack() },
                )
            }

            is NavRoute.Members -> {
                val store = koinInject<MembersStore> { parametersOf(route.householdId) }
                MembersScreen(
                    store = store,
                    onNavigateBack = { navigator.navigateBack() },
                )
            }

            is NavRoute.Activity -> {
                val store = koinInject<ActivityStore> { parametersOf(route.householdId) }
                ActivityScreen(
                    store = store,
                    onNavigateBack = { navigator.navigateBack() },
                )
            }

            is NavRoute.Settings -> {
                val store = koinInject<SettingsStore>()
                SettingsScreen(
                    store = store,
                    onNavigateToAuth = { navigator.replaceWith(NavRoute.Auth) },
                    onNavigateToHouseholdSetup = { navigator.replaceWith(NavRoute.HouseholdSetup) },
                    onNavigateBack = { navigator.navigateBack() },
                )
            }
        }
    }
}
