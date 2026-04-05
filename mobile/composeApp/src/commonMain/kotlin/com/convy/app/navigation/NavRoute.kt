package com.convy.app.navigation

sealed interface NavRoute {
    data object Auth : NavRoute
    data object HouseholdSetup : NavRoute
    data class HouseholdLists(val householdId: String) : NavRoute
    data class ListDetail(val householdId: String, val listId: String, val listName: String) : NavRoute
    data class CreateItem(val householdId: String, val listId: String) : NavRoute
    data class EditItem(val householdId: String, val listId: String, val itemId: String) : NavRoute
    data class Members(val householdId: String) : NavRoute
    data object Settings : NavRoute
}
