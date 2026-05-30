package com.convy.app.ui.screens.listdetail

import com.convy.shared.domain.model.ParsedItem
import com.convy.shared.domain.model.ParsedTask
import com.convy.shared.domain.repository.ItemRepository
import com.convy.shared.domain.repository.TaskRepository

class ListDetailVoiceCoordinator(
    private val itemRepository: ItemRepository,
    private val taskRepository: TaskRepository,
) {
    suspend fun parse(
        isTaskList: Boolean,
        listId: String,
        audioData: ByteArray,
    ): Result<ListDetailVoiceParseResult> =
        if (isTaskList) {
            taskRepository.parseVoiceAudio(listId, audioData).map { result ->
                ListDetailVoiceParseResult(
                    transcription = result.transcription,
                    tasks = result.tasks.map { parsed ->
                        ParsedVoiceTask(
                            title = parsed.title,
                            note = parsed.note,
                            assignedToUserId = parsed.assignedToUserId,
                            assignedToUserName = parsed.assignedToUserName,
                            dueDate = parsed.dueDate,
                            reminderAtUtc = parsed.reminderAtUtc,
                            priority = parsed.priority,
                            matchedExistingTask = parsed.matchedExistingTask,
                        )
                    },
                )
            }
        } else {
            itemRepository.parseVoiceAudio(listId, audioData).map { result ->
                ListDetailVoiceParseResult(
                    transcription = result.transcription,
                    items = result.items.map { parsed ->
                        ParsedVoiceItem(parsed.title, parsed.quantity, parsed.unit, parsed.matchedExistingItem)
                    },
                )
            }
        }

    suspend fun confirmItems(listId: String, selected: List<ParsedVoiceItem>): Result<Unit> {
        val parsedItems = selected.map {
            ParsedItem(it.title, it.quantity, it.unit, it.matchedExistingItem)
        }
        return itemRepository.batchCreate(listId, parsedItems, source = "voice").map { }
    }

    suspend fun confirmTasks(listId: String, selected: List<ParsedVoiceTask>): Result<Unit> {
        val parsedTasks = selected.map {
            ParsedTask(
                title = it.title,
                note = it.note,
                assignedToUserId = it.assignedToUserId,
                assignedToUserName = it.assignedToUserName,
                dueDate = it.dueDate,
                reminderAtUtc = it.reminderAtUtc,
                priority = it.priority,
                matchedExistingTask = it.matchedExistingTask,
            )
        }
        return taskRepository.batchCreate(listId, parsedTasks)
    }
}

data class ListDetailVoiceParseResult(
    val transcription: String,
    val items: List<ParsedVoiceItem> = emptyList(),
    val tasks: List<ParsedVoiceTask> = emptyList(),
)
