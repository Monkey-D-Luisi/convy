package com.convy.app.di

import com.convy.app.navigation.AppNavigator
import com.convy.app.ui.screens.auth.AuthStore
import com.convy.app.ui.screens.householdsetup.HouseholdSetupStore
import com.convy.app.ui.screens.item.ItemFormStore
import com.convy.app.ui.screens.listdetail.ListDetailStore
import com.convy.app.ui.screens.lists.HouseholdListsStore
import com.convy.app.ui.screens.members.MembersStore
import com.convy.app.ui.screens.settings.SettingsStore
import org.koin.dsl.module

val appModule = module {
    single { AppNavigator() }

    factory { AuthStore(get(), get(), get()) }
    factory { HouseholdSetupStore(get(), get()) }
    factory { SettingsStore(get()) }

    factory { (householdId: String) ->
        HouseholdListsStore(householdId, get(), get())
    }
    factory { (householdId: String, listId: String, listName: String) ->
        ListDetailStore(householdId, listId, listName, get())
    }
    factory { (householdId: String, listId: String, itemId: String?) ->
        ItemFormStore(householdId, listId, itemId, get())
    }
    factory { (householdId: String) ->
        MembersStore(householdId, get(), get())
    }
}
