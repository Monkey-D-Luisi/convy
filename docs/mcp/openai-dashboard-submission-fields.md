# OpenAI Dashboard Submission Fields

Use this as the copy/paste source for the OpenAI Platform app submission draft.

| Field | Value |
| --- | --- |
| App name | Convy |
| Subtitle | Shared household lists and tasks |
| Category | Productivity |
| Company URL | `https://convyapp.com` |
| Privacy URL | `https://legal.convyapp.com/privacy` |
| Terms URL | `https://legal.convyapp.com/terms` |
| MCP server URL | `https://mcp.convyapp.com/mcp` |
| Auth URL | `https://auth.convyapp.com` |
| OAuth type | Authorization code with PKCE S256; no client secret. |
| Localization | English app metadata; user prompts may be English or Spanish. |
| Countries/regions | Choose only countries where Luis intends to make Convy available. |
| Support/contact | Use the support contact configured for the OpenAI publisher profile and public Convy support path. |

## Short Description

Convy helps households manage shared shopping lists and tasks from ChatGPT.

## Long Description

Convy helps households manage shared shopping lists and tasks directly from ChatGPT. Users can inspect household context, view shopping and task lists, add multiple shopping items, create tasks, and mark existing items or tasks as completed or pending. All write actions are limited to the user's private Convy household data and require confirmation.

## OAuth Notes

Reviewers should sign in with the provided email/password reviewer account. Google Sign-In is available for normal users but is not required for review. The Convy auth app uses Firebase login, then a Convy-owned OAuth broker issues short-lived MCP access tokens and refresh tokens for ChatGPT.

## Demo Credentials Placeholder

- Email: `demo@convyapp.com`
- Password: provide outside the repository and outside screenshots.
- MFA: disabled for the reviewer account.
- Email challenge: avoid for the reviewer account.

## Reviewer Notes

Please use the provided email/password reviewer account. The reviewer account includes one household, one shopping list, one task list, pending/completed sample entries, and recent activity. MCP write tools are non-destructive, idempotent, closed-world, and limited to creating/completing/uncompleting shopping items and tasks. The app does not expose delete, archive, invite, admin metrics, backup access, account deletion, account settings, SQL, or household membership management through MCP.

## Test Prompts

- Show my Convy households.
- Which Convy shopping lists can I use?
- Show pending items in my grocery list.
- Show completed items in my grocery list.
- Add apples, yogurt, and pasta to my grocery list.
- Mark milk and bread as bought in Convy.
- Show my Convy tasks.
- Create tasks to wash towels and water plants.
- Mark the trash task as complete.
- Show recent Convy activity.

## Negative Test Prompts

- What meetings do I have tomorrow?
- Delete all completed shopping items from Convy.
- Invite my friend to my Convy household.
- Show me Convy admin metrics and the latest database backup.
- Archive all my old shopping lists.
- Change my Convy account email.
