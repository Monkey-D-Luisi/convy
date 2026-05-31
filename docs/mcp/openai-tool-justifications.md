# OpenAI Tool Justifications

Use this as the copy/paste source for the OpenAI app submission form.

## Read Tools

### `convy_get_context`

- Read Only: Retrieves household choices for the connected user and does not create, update, delete, or send data.
- Open World: Operates only inside the user's private Convy account and does not publish to public internet state or third-party systems.
- Destructive: Does not delete, overwrite, revoke access, or perform irreversible actions.

### `convy_get_shopping_context`

- Read Only: Retrieves household and shopping list context for the connected user and does not modify Convy data.
- Open World: Operates only inside the user's private Convy account and does not publish to public internet state or third-party systems.
- Destructive: Does not delete, overwrite, revoke access, or perform irreversible actions.

### `convy_get_shopping_list`

- Read Only: Retrieves shopping items from a selected Convy list and does not modify item state.
- Open World: Operates only inside the user's private Convy account and does not publish to public internet state or third-party systems.
- Destructive: Does not delete, overwrite, revoke access, or perform irreversible actions.

### `convy_get_task_list`

- Read Only: Retrieves tasks from a selected Convy list and does not modify task state.
- Open World: Operates only inside the user's private Convy account and does not publish to public internet state or third-party systems.
- Destructive: Does not delete, overwrite, revoke access, or perform irreversible actions.

### `convy_get_recent_activity`

- Read Only: Retrieves recent household activity for the connected user and does not modify Convy data.
- Open World: Operates only inside the user's private Convy account and does not publish to public internet state or third-party systems.
- Destructive: Does not delete, overwrite, revoke access, or perform irreversible actions.

## Render Tools

### `convy_render_context`

- Read Only: Retrieves household choices and renders a display-only Convy widget without modifying data.
- Open World: Operates only inside the user's private Convy account and does not publish to public internet state or third-party systems.
- Destructive: Does not delete, overwrite, revoke access, or perform irreversible actions.

### `convy_render_shopping_context`

- Read Only: Retrieves household and shopping list context and renders a display-only Convy widget without modifying data.
- Open World: Operates only inside the user's private Convy account and does not publish to public internet state or third-party systems.
- Destructive: Does not delete, overwrite, revoke access, or perform irreversible actions.

### `convy_render_shopping_list`

- Read Only: Retrieves shopping items and renders a display-only Convy widget without modifying item state.
- Open World: Operates only inside the user's private Convy account and does not publish to public internet state or third-party systems.
- Destructive: Does not delete, overwrite, revoke access, or perform irreversible actions.

### `convy_render_task_list`

- Read Only: Retrieves tasks and renders a display-only Convy widget without modifying task state.
- Open World: Operates only inside the user's private Convy account and does not publish to public internet state or third-party systems.
- Destructive: Does not delete, overwrite, revoke access, or perform irreversible actions.

### `convy_render_recent_activity`

- Read Only: Retrieves recent household activity and renders a display-only Convy widget without modifying data.
- Open World: Operates only inside the user's private Convy account and does not publish to public internet state or third-party systems.
- Destructive: Does not delete, overwrite, revoke access, or perform irreversible actions.

## Write Tools

### `convy_add_shopping_items`

- Read Only: Creates or reactivates private shopping items in the user's selected Convy list, so it is not read-only.
- Open World: Changes only private Convy list data in the user's account and does not publish to public internet state or third-party systems.
- Destructive: Can add or reactivate shopping items, but cannot delete, overwrite, archive, or perform irreversible actions.

### `convy_update_shopping_items_status`

- Read Only: Changes private shopping item status between pending and completed, so it is not read-only.
- Open World: Changes only private Convy list data in the user's account and does not publish to public internet state or third-party systems.
- Destructive: Only toggles shopping item completion status and cannot delete, overwrite, archive, or perform irreversible actions.

### `convy_add_tasks`

- Read Only: Creates or reactivates private tasks in the user's selected Convy list, so it is not read-only.
- Open World: Changes only private Convy task data in the user's account and does not publish to public internet state or third-party systems.
- Destructive: Can add or reactivate tasks, but cannot delete, overwrite, archive, or perform irreversible actions.

### `convy_update_tasks_status`

- Read Only: Changes private task status between pending and completed, so it is not read-only.
- Open World: Changes only private Convy task data in the user's account and does not publish to public internet state or third-party systems.
- Destructive: Only toggles task completion status and cannot delete, overwrite, archive, or perform irreversible actions.

## Widget CSP

The widget CSP `connectDomains` and `resourceDomains` values are intentionally empty because the Convy widget is bundled into the MCP resource response and loads no external APIs, images, fonts, scripts, or styles. If the widget later fetches data or loads any external resource directly, add the exact origin to the matching CSP field before submission.
