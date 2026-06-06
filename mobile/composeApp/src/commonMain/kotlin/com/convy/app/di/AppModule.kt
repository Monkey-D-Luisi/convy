package com.convy.app.di

import com.convy.app.navigation.AppNavigator
import com.convy.app.ui.screens.activity.ActivityStore
import com.convy.app.ui.screens.auth.AuthStore
import com.convy.app.ui.screens.households.HouseholdsStore
import com.convy.app.ui.screens.householdsetup.HouseholdSetupStore
import com.convy.app.ui.screens.item.ItemFormStore
import com.convy.app.ui.screens.listdetail.ListDetailEntryActions
import com.convy.app.ui.screens.listdetail.ListDetailStore
import com.convy.app.ui.screens.listdetail.ListDetailVoiceCoordinator
import com.convy.app.ui.screens.lists.HouseholdListsStore
import com.convy.app.ui.screens.members.MembersStore
import com.convy.app.ui.screens.settings.SettingsStore
import com.convy.app.ui.screens.startup.StartupStore
import com.convy.app.ui.screens.task.TaskFormStore
import org.koin.dsl.module

val appModule = module {
    single { AppNavigator() }
    single { ListDetailEntryActions(get(), get()) }
    single { ListDetailVoiceCoordinator(get(), get()) }

    factory { AuthStore(get(), get(), get(), get(), get()) }
    factory { StartupStore(get(), get(), get(), get(), get()) }
    factory { HouseholdSetupStore(get(), get(), get()) }

    factory { (householdId: String) ->
        HouseholdListsStore(householdId, get(), get(), get(), get(), get(), get())
    }
    factory { (activeHouseholdId: String?) ->
        HouseholdsStore(activeHouseholdId, get(), get(), get())
    }
    factory { (householdId: String) ->
        SettingsStore(householdId, get(), get(), get(), get(), get())
    }
    factory { (householdId: String, listId: String, listName: String, listType: String) ->
        ListDetailStore(householdId, listId, listName, listType, get(), get(), get(), get(), get(), get(), get(), get())
    }
    factory { (householdId: String, listId: String, itemId: String?) ->
        ItemFormStore(householdId, listId, itemId, get(), get())
    }
    factory { (householdId: String, listId: String, taskId: String?) ->
        TaskFormStore(householdId, listId, taskId, get(), get())
    }
    factory { (householdId: String) ->
        MembersStore(householdId, get(), get())
    }
    factory { (householdId: String) ->
        ActivityStore(householdId, get(), get())
    }
}
