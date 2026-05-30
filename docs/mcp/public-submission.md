# ChatGPT Public App Submission Prep

Convy MCP cannot be used outside Developer Mode until the ChatGPT app is submitted, reviewed, approved, and published by OpenAI.

## Account Requirements

- Submit from an OpenAI Platform project that can submit ChatGPT apps.
- Complete publisher identity verification.
- Use production-like public URLs and legal pages.
- Have a stable demo account and sample Convy data.

## Production-Like URLs

- MCP URL: `https://mcp.convyapp.com/mcp`
- Auth URL: `https://auth.convyapp.com`
- Privacy URL: `https://legal.convyapp.com/privacy`
- Terms URL: `https://legal.convyapp.com/terms`
- Company URL: `https://convyapp.com`

## Submission Assets

Prepare:

- App name: `Convy`
- Subtitle: `Shared household lists and tasks`
- Category: productivity
- Description: Convy helps households manage shared shopping lists and tasks directly from ChatGPT. Users can inspect household context, view shopping and task lists, add multiple shopping items, create tasks, and mark existing items or tasks as completed or pending. All write actions are limited to the user's private Convy household data and require confirmation.
- Logo and screenshots from ChatGPT web/mobile test runs.
- Demo account using email/password unless reviewer guidance explicitly accepts Google Sign-In.
- Demo data:
  - one household
  - one shopping list with pending and completed items
  - one task list with pending and completed tasks
  - recent activity

## Tool Review Checklist

- Tools advertise correct annotations.
- Write tools are non-destructive, idempotent, and closed-world.
- Tool outputs are relevant, concise, and structured.
- Tool outputs do not include secrets, tokens, admin metrics, backups, or unnecessary personal identifiers.
- Write tools require user confirmation in ChatGPT.
- API enforces idempotency for MCP writes.
- Metadata advertises only the five read scopes plus `convy.items.write` and `convy.tasks.write`.

## Test Prompts

- "Show my Convy households."
- "Show my shopping lists."
- "Show pending items in my shopping list."
- "Show completed items in my shopping list."
- "Show my tasks."
- "Show recent household activity."
- "Add milk to my shopping list."
- "Mark milk as complete."
- "Create a task to clean the kitchen."
- "Mark the kitchen task as complete."
- "After revocation, confirm Convy data is unavailable."

## Review Risks

- OAuth demo login must succeed without MFA, SMS, or external account challenge.
- Firebase authorized domains must include `auth.convyapp.com`.
- MCP endpoint must be publicly reachable.
- Legal docs must disclose ChatGPT MCP data access and revocation.
- Public site and legal pages must be reachable.
- React Apps SDK widget must render through `ui://widget/convy-summary-v1.html`.
- Dashboard/admin-only features must not be exposed through MCP.
- Offsite encrypted backups require restic environment configuration before operators present them as active for a deployment.

## References

- Connect from ChatGPT: `https://developers.openai.com/apps-sdk/deploy/connect-chatgpt`
- Submit your app: `https://developers.openai.com/apps-sdk/deploy/submission`
- App submission guidelines: `https://developers.openai.com/apps-sdk/app-submission-guidelines`
