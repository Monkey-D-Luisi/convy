# ChatGPT Public App Submission Prep

Convy MCP cannot be used without Developer Mode until the ChatGPT app is submitted, reviewed, approved, and published by OpenAI.

## OpenAI Account Requirements

- Submit from an OpenAI Platform project with global data residency.
- Complete publisher identity verification in the OpenAI Dashboard.
- Ensure the submitting user can create and submit Apps SDK apps.

## Production-Like URLs

Use the `convyapp.com` cutover URLs:

- MCP URL: `https://mcp.convyapp.com/mcp`
- Auth URL: `https://auth.convyapp.com`
- Privacy URL: `https://legal.convyapp.com/privacy`
- Terms URL: `https://legal.convyapp.com/terms`
- Company URL: `https://convyapp.com`

## Submission Assets

Prepare:

- App name: `Convy`
- Description: access Convy household context, shopping lists, tasks, and recent activity from ChatGPT, with limited creation and completion actions for shopping items and tasks.
- Logo and screenshots from ChatGPT web and mobile test runs.
- Demo account with no MFA, no SMS, no email challenge, and sample data:
  - one household
  - one shopping list with pending and completed items
  - one task list with pending and completed tasks
  - recent activity
- Demo login should use email/password for review unless OpenAI explicitly accepts a Google test account. Google Sign-In can stay enabled for real users, but review credentials should avoid extra Google account challenges.
- Test prompts and expected results:
  - "Show my Convy households."
  - "Show my shopping lists."
  - "Show pending items in my shopping list."
  - "Show my tasks."
  - "Show recent household activity."
  - "Add milk to my shopping list."
  - "Mark milk as complete."
  - "Create a task to clean the kitchen."
  - "Mark the kitchen task as complete."
  - "After revocation, confirm Convy data is unavailable."

## Review Risks To Check Before Submission

- MCP endpoint must be reachable from outside the company network.
- OAuth demo login must succeed without additional configuration.
- The app must return only data disclosed in the privacy policy.
- Tool outputs must be relevant, concise, and free of unnecessary personal identifiers.
- Metadata must advertise only the five read scopes plus `convy.items.write` and `convy.tasks.write`.
- Write tool annotations must be non-destructive, closed-world, and idempotent.
- Write tools must require ChatGPT confirmation and API idempotency.

## References

- Connect from ChatGPT: https://developers.openai.com/apps-sdk/deploy/connect-chatgpt
- Submit your app: https://developers.openai.com/apps-sdk/deploy/submission
- App submission guidelines: https://developers.openai.com/apps-sdk/app-submission-guidelines
