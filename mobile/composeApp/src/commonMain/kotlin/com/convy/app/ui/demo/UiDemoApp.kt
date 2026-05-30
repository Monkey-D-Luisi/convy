package com.convy.app.ui.demo

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.ExperimentalComposeUiApi
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.testTagsAsResourceId
import com.convy.app.ui.screens.activity.ActivityContent
import com.convy.app.ui.screens.activity.ActivityIntent
import com.convy.app.ui.screens.activity.ActivityState
import com.convy.app.ui.screens.activity.DateGroup
import com.convy.app.ui.screens.auth.AuthContent
import com.convy.app.ui.screens.auth.AuthIntent
import com.convy.app.ui.screens.auth.AuthState
import com.convy.app.ui.screens.households.HouseholdsContent
import com.convy.app.ui.screens.households.HouseholdsIntent
import com.convy.app.ui.screens.households.HouseholdsState
import com.convy.app.ui.screens.householdsetup.HouseholdSetupContent
import com.convy.app.ui.screens.householdsetup.HouseholdSetupIntent
import com.convy.app.ui.screens.householdsetup.HouseholdSetupState
import com.convy.app.ui.screens.item.ItemFormContent
import com.convy.app.ui.screens.item.ItemFormIntent
import com.convy.app.ui.screens.item.ItemFormState
import com.convy.app.ui.screens.listdetail.ListDetailContent
import com.convy.app.ui.screens.listdetail.ListDetailIntent
import com.convy.app.ui.screens.listdetail.ListDetailState
import com.convy.app.ui.screens.listdetail.ListEntryUi
import com.convy.app.ui.screens.lists.HouseholdListsContent
import com.convy.app.ui.screens.lists.HouseholdListsIntent
import com.convy.app.ui.screens.lists.HouseholdListsState
import com.convy.app.ui.screens.members.MembersContent
import com.convy.app.ui.screens.members.MembersIntent
import com.convy.app.ui.screens.members.MembersState
import com.convy.app.ui.screens.settings.SettingsContent
import com.convy.app.ui.screens.settings.SettingsIntent
import com.convy.app.ui.screens.settings.SettingsState
import com.convy.app.ui.screens.task.TaskAssigneeUi
import com.convy.app.ui.screens.task.TaskFormContent
import com.convy.app.ui.screens.task.TaskFormIntent
import com.convy.app.ui.screens.task.TaskFormState
import com.convy.app.ui.theme.ConvyTheme
import com.convy.shared.domain.model.ActivityLogEntry
import com.convy.shared.domain.model.DuplicateItem
import com.convy.shared.domain.model.Household
import com.convy.shared.domain.model.HouseholdList
import com.convy.shared.domain.model.HouseholdMember
import com.convy.shared.domain.model.HouseholdRole
import com.convy.shared.domain.model.Invite
import com.convy.shared.domain.model.ListType
import com.convy.shared.domain.model.NotificationPreferences
import com.convy.shared.domain.model.TaskPriority
import kotlinx.datetime.LocalDate
import kotlinx.datetime.LocalDateTime

enum class UiDemoRoute(val id: String) {
    Auth("auth"),
    HouseholdSetup("household-setup"),
    HouseholdLists("household-lists"),
    Households("households"),
    ShoppingListDetail("list-detail-shopping"),
    TaskListDetail("list-detail-tasks"),
    ItemForm("item-form"),
    TaskForm("task-form"),
    Members("members"),
    Activity("activity"),
    Settings("settings"),
    ;

    companion object {
        fun fromId(id: String?): UiDemoRoute =
            entries.firstOrNull { it.id == id } ?: Auth
    }
}

@OptIn(ExperimentalComposeUiApi::class)
@Composable
fun UiDemoApp(routeId: String?) {
    Box(Modifier.semantics { testTagsAsResourceId = true }) {
        ConvyTheme {
            when (UiDemoRoute.fromId(routeId)) {
                UiDemoRoute.Auth -> AuthContent(
                    state = AuthState(email = "luisa@example.com", password = "12345678"),
                    onIntent = {},
                )
                UiDemoRoute.HouseholdSetup -> HouseholdSetupContent(
                    state = HouseholdSetupState(householdName = "Rivas Home"),
                    onIntent = {},
                )
                UiDemoRoute.HouseholdLists -> HouseholdListsContent(
                    state = demoHouseholdListsState(),
                    onIntent = {},
                )
                UiDemoRoute.Households -> HouseholdsContent(
                    state = demoHouseholdsState(),
                    onIntent = {},
                )
                UiDemoRoute.ShoppingListDetail -> ListDetailContent(
                    state = demoShoppingListDetailState(),
                    onIntent = {},
                )
                UiDemoRoute.TaskListDetail -> ListDetailContent(
                    state = demoTaskListDetailState(),
                    onIntent = {},
                )
                UiDemoRoute.ItemForm -> ItemFormContent(
                    state = demoItemFormState(),
                    onIntent = {},
                )
                UiDemoRoute.TaskForm -> TaskFormContent(
                    state = demoTaskFormState(),
                    onIntent = {},
                )
                UiDemoRoute.Members -> MembersContent(
                    state = demoMembersState(),
                    onIntent = {},
                )
                UiDemoRoute.Activity -> ActivityContent(
                    state = demoActivityState(),
                    onIntent = {},
                )
                UiDemoRoute.Settings -> SettingsContent(
                    state = demoSettingsState(),
                    onIntent = {},
                )
            }
        }
    }
}

private fun demoHouseholdListsState() = HouseholdListsState(
    householdId = "home",
    householdName = "Rivas Home",
    households = demoHouseholds(),
    lists = listOf(
        HouseholdList("groceries", "Weekly groceries", ListType.Shopping, "home", "luisa", "2026-05-01T10:00:00Z", false, null),
        HouseholdList("chores", "House chores", ListType.Tasks, "home", "luis", "2026-05-03T09:00:00Z", false, null),
        HouseholdList("pharmacy", "Pharmacy", ListType.Shopping, "home", "luisa", "2026-05-05T17:00:00Z", false, null),
        HouseholdList("weekend", "Weekend errands", ListType.Tasks, "home", "luis", "2026-05-06T12:00:00Z", false, null),
    ),
    pendingCounts = mapOf(
        "groceries" to 8,
        "chores" to 3,
        "pharmacy" to 2,
        "weekend" to 4,
    ),
)

private fun demoHouseholdsState() = HouseholdsState(
    activeHouseholdId = "home",
    households = demoHouseholds(),
)

private fun demoShoppingListDetailState() = ListDetailState(
    listId = "groceries",
    householdId = "home",
    listName = "Weekly groceries",
    listType = "Shopping",
    pendingEntries = listOf(
        ListEntryUi(
            id = "milk",
            title = "Oat milk",
            note = "Barista edition",
            listId = "groceries",
            createdByName = "Luisa",
            createdAt = "2026-05-30T09:15:00Z",
            isCompleted = false,
            completedByName = null,
            completedAt = null,
            returnedToPendingByName = null,
            returnedToPendingAt = null,
            quantity = 2,
            unit = "cartons",
        ),
        ListEntryUi(
            id = "tomatoes",
            title = "Cherry tomatoes",
            note = "For dinner",
            listId = "groceries",
            createdByName = "Luis",
            createdAt = "2026-05-30T09:40:00Z",
            isCompleted = false,
            completedByName = null,
            completedAt = null,
            returnedToPendingByName = null,
            returnedToPendingAt = null,
            quantity = 1,
            unit = "pack",
        ),
        ListEntryUi(
            id = "detergent",
            title = "Laundry detergent",
            note = null,
            listId = "groceries",
            createdByName = "Luisa",
            createdAt = "2026-05-29T18:20:00Z",
            isCompleted = false,
            completedByName = null,
            completedAt = null,
            returnedToPendingByName = null,
            returnedToPendingAt = null,
            quantity = 1,
            unit = "bottle",
            recurrenceFrequency = "monthly",
        ),
    ),
    completedEntries = listOf(
        ListEntryUi(
            id = "bread",
            title = "Sourdough bread",
            note = null,
            listId = "groceries",
            createdByName = "Luis",
            createdAt = "2026-05-29T15:10:00Z",
            isCompleted = true,
            completedByName = "Luisa",
            completedAt = "2026-05-30T10:05:00Z",
            returnedToPendingByName = null,
            returnedToPendingAt = null,
            quantity = 1,
            unit = "loaf",
        ),
    ),
    showCompleted = true,
    pendingSyncCount = 2,
)

private fun demoTaskListDetailState() = ListDetailState(
    listId = "chores",
    householdId = "home",
    listName = "House chores",
    listType = "Tasks",
    pendingEntries = listOf(
        ListEntryUi(
            id = "plants",
            title = "Water balcony plants",
            note = "Check the basil first",
            listId = "chores",
            createdByName = "Luisa",
            createdAt = "2026-05-30T08:00:00Z",
            isCompleted = false,
            completedByName = null,
            completedAt = null,
            returnedToPendingByName = null,
            returnedToPendingAt = null,
            assignedToUserName = "Luis",
            dueDate = "2026-05-31",
            reminderAtUtc = "2026-05-31T08:00:00Z",
            priority = TaskPriority.High,
        ),
        ListEntryUi(
            id = "vacuum",
            title = "Vacuum living room",
            note = null,
            listId = "chores",
            createdByName = "Luis",
            createdAt = "2026-05-29T20:00:00Z",
            isCompleted = false,
            completedByName = null,
            completedAt = null,
            returnedToPendingByName = null,
            returnedToPendingAt = null,
            assignedToUserName = "Luisa",
            dueDate = "2026-06-01",
            priority = TaskPriority.Normal,
        ),
    ),
    completedEntries = listOf(
        ListEntryUi(
            id = "recycling",
            title = "Take out recycling",
            note = null,
            listId = "chores",
            createdByName = "Luis",
            createdAt = "2026-05-27T07:30:00Z",
            isCompleted = true,
            completedByName = "Luis",
            completedAt = "2026-05-30T08:20:00Z",
            returnedToPendingByName = null,
            returnedToPendingAt = null,
            assignedToUserName = "Luis",
            priority = TaskPriority.Low,
        ),
    ),
    showCompleted = true,
)

private fun demoItemFormState() = ItemFormState(
    listId = "groceries",
    householdId = "home",
    itemId = "tomatoes",
    title = "Cherry tomatoes",
    quantity = "1",
    unit = "pack",
    note = "Small pack, not the large tray",
    isEditing = true,
    suggestions = listOf("Eggs", "Greek yogurt", "Coffee beans"),
    duplicateWarning = listOf(DuplicateItem("tomatoes-2", "Tomatoes", 2, "kg")),
    recurrenceFrequency = 1,
    recurrenceInterval = 1,
)

private fun demoTaskFormState() = TaskFormState(
    listId = "chores",
    householdId = "home",
    taskId = "plants",
    title = "Water balcony plants",
    note = "Check the basil first and trim dry leaves.",
    isEditing = true,
    assignees = listOf(
        TaskAssigneeUi("luisa", "Luisa"),
        TaskAssigneeUi("luis", "Luis"),
    ),
    assignedToUserId = "luis",
    assignedToUserName = "Luis",
    dueDate = LocalDate(2026, 5, 31),
    reminderLocalDateTime = LocalDateTime(2026, 5, 31, 8, 0),
    priority = TaskPriority.High,
)

private fun demoMembersState() = MembersState(
    householdId = "home",
    members = listOf(
        HouseholdMember("luisa", "Luisa", "luisa@example.com", HouseholdRole.Owner, "2026-03-10T12:00:00Z"),
        HouseholdMember("luis", "Luis", "luis@example.com", HouseholdRole.Member, "2026-03-10T12:05:00Z"),
    ),
    invite = Invite("invite-1", "home", "RIVAS-42", "2026-06-03", true, "2026-05-30T10:00:00Z"),
    activeInvites = listOf(
        Invite("invite-2", "home", "HOME-88", "2026-06-05", true, "2026-05-29T10:00:00Z"),
    ),
)

private fun demoActivityState() = ActivityState(
    householdId = "home",
    groupedEntries = listOf(
        DateGroup(
            date = "Today",
            entries = listOf(
                ActivityLogEntry("a1", "home", "ListItem", "milk", "Created", "luisa", "Luisa", "2026-05-30T09:15:00Z", null),
                ActivityLogEntry("a2", "home", "ListItem", "bread", "Completed", "luis", "Luis", "2026-05-30T10:05:00Z", null),
                ActivityLogEntry("a3", "home", "TaskItem", "plants", "Updated", "luisa", "Luisa", "2026-05-30T10:30:00Z", null),
            ),
        ),
        DateGroup(
            date = "2026-05-29",
            entries = listOf(
                ActivityLogEntry("a4", "home", "HouseholdMember", "luis", "MemberJoined", "luis", "Luis", "2026-05-29T17:20:00Z", null),
            ),
        ),
    ),
    hasMore = false,
)

private fun demoSettingsState() = SettingsState(
    displayName = "Luisa",
    email = "luisa@example.com",
    householdName = "Rivas Home",
    householdId = "home",
    appVersion = "0.1.23",
    renameText = "Rivas Home",
    notificationPreferences = NotificationPreferences(
        itemsAdded = true,
        tasksAdded = true,
        itemsCompleted = false,
        tasksCompleted = false,
        taskReminders = true,
        itemTaskChanges = false,
        listChanges = true,
        memberChanges = true,
    ),
)

private fun demoHouseholds() = listOf(
    Household("home", "Rivas Home", "luisa", "2026-03-10T12:00:00Z"),
    Household("parents", "Parents", "luisa", "2026-04-02T18:00:00Z"),
)
