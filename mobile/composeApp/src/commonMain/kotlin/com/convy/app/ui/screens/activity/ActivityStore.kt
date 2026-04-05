package com.convy.app.ui.screens.activity

import com.convy.shared.data.remote.HouseholdEvent
import com.convy.shared.data.remote.HouseholdRealtimeService
import com.convy.shared.domain.repository.ActivityRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

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

    init {
        loadActivity()
        observeRealtimeEvents()
    }

    fun processIntent(intent: ActivityIntent) {
        when (intent) {
            is ActivityIntent.Refresh -> loadActivity()
            is ActivityIntent.NavigateBack -> scope.launch {
                _sideEffects.emit(ActivitySideEffect.NavigateBack)
            }
        }
    }

    private fun loadActivity() {
        _state.update { it.copy(isLoading = true, error = null) }
        scope.launch {
            activityRepository.getByHousehold(householdId).fold(
                onSuccess = { entries ->
                    _state.update { it.copy(entries = entries, isLoading = false) }
                },
                onFailure = { error ->
                    _state.update {
                        it.copy(isLoading = false, error = error.message ?: "Failed to load activity")
                    }
                },
            )
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
                    is HouseholdEvent.ListCreated,
                    is HouseholdEvent.ListRenamed,
                    is HouseholdEvent.ListArchived,
                    is HouseholdEvent.MemberJoined -> loadActivity()
                }
            }
        }
    }
}
