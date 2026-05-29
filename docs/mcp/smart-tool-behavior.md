# Smart Tool Behavior

This document describes implemented smart-write behavior for ChatGPT MCP item and task creation. It does not describe natural-language reasoning performed by the backend.

## Client Guidance

ChatGPT should:

- extract only products or tasks the user clearly asked to add
- support Spanish and English user phrasing
- ignore filler words
- respect negations
- respect corrections
- send multiple requested items/tasks in one batch call
- ask the user to choose a household or list when tool output marks selection as required
- avoid inventing quantities, units, notes, item IDs, task IDs, households, or lists

Example shopping write:

```json
{
  "listId": "11111111-1111-4111-8111-111111111111",
  "items": [
    { "title": "Leche" },
    { "title": "Pan" },
    { "title": "Huevos" }
  ]
}
```

If the user says "No compres pan, anade huevos", the call should include only `Huevos`.

If the user says "Anade leche, perdon, bebida de avena", the call should include only `Bebida de avena`.

Quantities and units must be explicit. Because Convy quantity is currently an integer, fractional quantities should be represented in `unit` or `note`, for example:

```json
{ "title": "Tomates", "unit": "medio kilo" }
```

## Backend Guarantees

The backend:

- trims whitespace
- collapses internal whitespace
- applies stable display capitalization
- stores `normalized_title`
- matches exact normalized titles only
- reuses pending matches
- returns completed matches to pending
- reports quantity/unit/note conflicts as warnings
- reports duplicates inside the same batch

The API does not:

- translate titles
- run fuzzy matching
- infer missing quantities or units
- edit existing quantity/unit/note values silently
- choose between multiple plausible lists
- delete or archive items/tasks

## Current Scope

Implemented smart batch paths:

- `POST /api/v1/lists/{listId}/items/smart-batch`
- `POST /api/v1/lists/{listId}/tasks/smart-batch`

Implemented status batch paths:

- `POST /api/v1/lists/{listId}/items/status-batch`
- `POST /api/v1/lists/{listId}/tasks/status-batch`

Future smart behavior should be documented as backlog until implemented and covered by tests.
