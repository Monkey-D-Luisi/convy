# Smart Shopping Behavior

`convy_add_shopping_items` is the preferred write path for adding products from ChatGPT.

## Client Guidance

ChatGPT should extract only products the user clearly asked to add. It should support Spanish and English, ignore filler words, respect negations and corrections, and send multiple requested products in one tool call.

Examples:

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

If the user says "No compres pan, añade huevos", the call must include only `Huevos`.

If the user says "Añade leche, perdón, bebida de avena", the call must include only `Bebida de avena`.

Quantities and units must be explicit. Because Convy quantity is currently an integer, fractional quantities should be represented in `unit` or `note`, for example `{ "title": "Tomates", "unit": "medio kilo" }`.

## API Guarantees

The backend normalizes titles for every channel, not only MCP. It trims whitespace, collapses internal whitespace, applies stable display capitalization, and stores a comparison key in `normalized_title`.

The smart-batch endpoint performs exact normalized matching only. It does not translate, run fuzzy matching, or interpret natural language. Pending matches are reused, completed matches are returned to pending, and conflicts in details are reported as warnings without silently editing the existing item.

The same behavior is mirrored for task smart-batch writes.
