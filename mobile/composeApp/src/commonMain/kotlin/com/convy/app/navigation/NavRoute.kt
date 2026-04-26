package com.convy.app.navigation

sealed interface NavRoute {
    data object Auth : NavRoute
    data object HouseholdSetup : NavRoute
    data class HouseholdLists(val householdId: String) : NavRoute
    data class ListDetail(val householdId: String, val listId: String, val listName: String, val listType: String) : NavRoute
    data class CreateItem(val householdId: String, val listId: String) : NavRoute
    data class EditItem(val householdId: String, val listId: String, val itemId: String) : NavRoute
    data class CreateTask(val householdId: String, val listId: String) : NavRoute
    data class EditTask(val householdId: String, val listId: String, val taskId: String) : NavRoute
    data class Members(val householdId: String) : NavRoute
    data class Activity(val householdId: String) : NavRoute
    data object Settings : NavRoute
}
