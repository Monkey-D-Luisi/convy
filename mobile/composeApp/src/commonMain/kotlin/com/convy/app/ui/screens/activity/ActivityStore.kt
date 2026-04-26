package com.convy.app.ui.screens.activity

import com.convy.app.generated.resources.*
import com.convy.app.util.UiText
import com.convy.shared.data.remote.HouseholdEvent
import com.convy.shared.data.remote.HouseholdRealtimeService
import com.convy.shared.domain.model.ActivityLogEntry
import com.convy.shared.domain.repository.ActivityRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import kotlinx.datetime.Clock
import kotlinx.datetime.TimeZone
import kotlinx.datetime.toLocalDateTime

class ActivityStore(
    private val householdId: String,
    private val activityRepository: ActivityRepository,
    private val realtimeService: HouseholdRealtimeService,
) {
    private val scope = CoroutineScope(Dispatchers.Main)
    private val _state = MutableStateFlow(ActivityState(householdId = householdId))
    val state: StateFlow<ActivityState> = _state.asStateFlow()

    private val _sideEffects = MutableSharedFlow<ActivitySideEffect>()
    val sideEffects: SharedFlow<ActivitySideEffect> = _sideEffects.asSharedFlow()

    private val allEntries = mutableListOf<ActivityLogEntry>()

    init {
        loadActivity()
        observeRealtimeEvents()
    }

    fun processIntent(intent: ActivityIntent) {
        when (intent) {
            is ActivityIntent.Refresh -> loadActivity()
            is ActivityIntent.LoadMore -> loadMore()
            is ActivityIntent.NavigateBack -> scope.launch {
                _sideEffects.emit(ActivitySideEffect.NavigateBack)
            }
        }
    }

    private fun loadActivity() {
        _state.update { it.copy(isLoading = true, error = null) }
        allEntries.clear()
        scope.launch {
            activityRepository.getByHousehold(householdId, limit = 50).fold(
                onSuccess = { entries ->
                    allEntries.addAll(entries)
                    _state.update {
                        it.copy(
                            groupedEntries = groupByDate(allEntries),
                            isLoading = false,
                            hasMore = entries.size >= 50,
                        )
                    }
                },
                onFailure = { error ->
                    _state.update {
                        it.copy(isLoading = false, error = UiText.fromError(error.message, Res.string.activity_load_failed))
                    }
                },
            )
        }
    }

    private fun loadMore() {
        if (_state.value.isLoadingMore || !_state.value.hasMore) return
        val lastEntry = allEntries.lastOrNull() ?: return
        _state.update { it.copy(isLoadingMore = true) }
        scope.launch {
            activityRepository.getByHousehold(householdId, limit = 50, before = lastEntry.createdAt).fold(
                onSuccess = { entries ->
                    allEntries.addAll(entries)
                    _state.update {
                        it.copy(
                            groupedEntries = groupByDate(allEntries),
                            isLoadingMore = false,
                            hasMore = entries.size >= 50,
                        )
                    }
                },
                onFailure = {
                    _state.update { it.copy(isLoadingMore = false) }
                },
            )
        }
    }

    private fun groupByDate(entries: List<ActivityLogEntry>): List<DateGroup> {
        val today = Clock.System.now()
            .toLocalDateTime(TimeZone.currentSystemDefault())
            .date.toString()
        return entries.groupBy { it.createdAt.take(10) }
            .map { (date, items) ->
                val label = when (date) {
                    today -> "Today"
                    else -> date
                }
                DateGroup(label, items)
            }
    }

    private fun observeRealtimeEvents() {
        scope.launch {
            realtimeService.events.collect { event ->
                when (event) {
                    is HouseholdEvent.ItemCreated,
                    is HouseholdEvent.ItemUpdated,
                    is HouseholdEvent.ItemCompleted,
                    is HouseholdEvent.ItemUncompleted,
                    is HouseholdEvent.ItemDeleted,
                    is HouseholdEvent.TaskCreated,
                    is HouseholdEvent.TaskUpdated,
                    is HouseholdEvent.TaskCompleted,
                    is HouseholdEvent.TaskUncompleted,
                    is HouseholdEvent.TaskDeleted,
                    is HouseholdEvent.ListCreated,
                    is HouseholdEvent.ListRenamed,
                    is HouseholdEvent.ListArchived,
                    is HouseholdEvent.MemberJoined,
                    is HouseholdEvent.HouseholdRenamed,
                    is HouseholdEvent.MemberLeft -> loadActivity()
                }
            }
        }
    }
}
