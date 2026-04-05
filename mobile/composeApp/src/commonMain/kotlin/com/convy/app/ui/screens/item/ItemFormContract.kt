package com.convy.app.ui.screens.item

import com.convy.shared.domain.model.DuplicateItem

data class ItemFormState(
    val listId: String = "",
    val householdId: String = "",
    val itemId: String? = null,
    val title: String = "",
    val quantity: String = "",
    val unit: String = "",
    val note: String = "",
    val isEditing: Boolean = false,
    val isLoading: Boolean = false,
    val isSaving: Boolean = false,
    val error: String? = null,
    val duplicateWarning: List<DuplicateItem> = emptyList(),
    val suggestions: List<String> = emptyList(),
)

sealed interface ItemFormIntent {
    data class UpdateTitle(val title: String) : ItemFormIntent
    data class UpdateQuantity(val quantity: String) : ItemFormIntent
    data class UpdateUnit(val unit: String) : ItemFormIntent
    data class UpdateNote(val note: String) : ItemFormIntent
    data class SelectSuggestion(val title: String) : ItemFormIntent
    data object Save : ItemFormIntent
    data object Delete : ItemFormIntent
    data object NavigateBack : ItemFormIntent
    data object DismissDuplicateWarning : ItemFormIntent
}

sealed interface ItemFormSideEffect {
    data object NavigateBack : ItemFormSideEffect
    data class ShowError(val message: String) : ItemFormSideEffect
}
