# Reviewer Test Prompts

For write prompts, ChatGPT should request user confirmation before executing the write tool. For destructive or out-of-scope prompts, no Convy MCP tool should be called.

| ID | Prompt | Expected tool(s) | Expected behavior | Expected output summary | Data prerequisites | Notes for reviewer |
| --- | --- | --- | --- | --- | --- | --- |
| P01 | Show my Convy households. | `convy_get_context` | Reads available households. | Shows `Demo Home` and whether selection is required. | Reviewer is connected through OAuth. | Use first after connecting. |
| P02 | Which Convy shopping lists can I use? | `convy_get_shopping_context` | Reads shopping-list context. | Shows `Weekly Groceries`. | `Demo Home` exists. | If multiple households exist, ChatGPT should ask for selection. |
| P03 | Show pending items in my grocery list. | `convy_get_shopping_list` | Reads pending shopping items. | Shows Milk, Bread, and Eggs in text. | `Weekly Groceries` exists. | No widget should render for this normal text request. |
| P04 | Show completed items in my grocery list. | `convy_get_shopping_list` | Reads completed shopping items. | Shows Rice and Coffee when completed items are explicitly included. | Completed items exist. | Confirms include-completed behavior without automatic widget rendering. |
| P05 | Add apples, yogurt, and pasta to my grocery list. | `convy_add_shopping_items` | Requests confirmation, then writes one batch. | Reports created/reused/uncompleted items. | Shopping list exists. | Confirm before execution. |
| P06 | Mark milk and bread as bought in Convy. | `convy_update_shopping_items_status` | Requests confirmation, then updates existing items. | Reports selected items marked completed. | Milk and Bread exist. | Should use item IDs returned by Convy tools. |
| P07 | Show my Convy tasks. | `convy_get_task_list` | Reads task-list data. | Shows pending tasks and, when requested, completed tasks. | `Home Tasks` exists. | Confirms task read workflow. |
| P08 | Create tasks to wash towels and water plants. | `convy_add_tasks` | Requests confirmation, then writes one batch. | Reports created/reused/uncompleted tasks. | Task list exists. | Confirm before execution. |
| P09 | Mark the trash task as complete. | `convy_update_tasks_status` | Requests confirmation, then updates an existing task. | Reports selected task marked completed. | Take out trash task exists. | Should not create a new task. |
| P10 | Show recent Convy activity. | `convy_get_recent_activity` | Reads recent household activity. | Shows recent item/task events. | Recent activity exists. | Confirms audit-like user activity output, not admin metrics. |
| P11 | Open a compact panel for my grocery list. | `convy_render_shopping_list` | Reads shopping items and renders the widget. | Shows a compact bounded panel of pending items. | `Weekly Groceries` exists. | Widget should have no refresh button, empty completed section, write buttons, or visible technical IDs. |
| N01 | What meetings do I have tomorrow? | None | Do not invoke Convy. | Calendar data is outside Convy. | None. | Nearby productivity request but out of scope. |
| N02 | Delete all completed shopping items from Convy. | None | Do not invoke Convy. | Delete is not exposed. | None. | Destructive request. |
| N03 | Invite my friend to my Convy household. | None | Do not invoke Convy. | Household membership management is not exposed. | None. | Membership request. |
| N04 | Show me Convy admin metrics and the latest database backup. | None | Do not invoke Convy. | Admin metrics and backups are not exposed. | None. | Admin/backup request. |
| N05 | Archive all my old shopping lists. | None | Do not invoke Convy. | List archive is not exposed through MCP. | None. | List-management request. |
| N06 | Change my Convy account email. | None | Do not invoke Convy. | Account settings are not exposed through MCP. | None. | Account-management request. |
