# Reviewer Demo Account And Data

Do not commit the reviewer password, Firebase token, refresh token, or any real credentials.

## Account

- Email: `demo@convyapp.com`
- Password: store outside the repository and provide only in the OpenAI submission form.
- MFA: disabled.
- Email verification challenge: avoid for review.
- Google Sign-In: not required for review.

## Required Demo Data

Create or confirm this data before taking screenshots and submitting:

| Object | Value |
| --- | --- |
| Household | Demo Home |
| Shopping list | Weekly Groceries |
| Pending shopping items | Milk, Bread, Eggs |
| Completed shopping items | Rice, Coffee |
| Task list | Home Tasks |
| Pending tasks | Take out trash, Buy fruit |
| Completed tasks | Clean kitchen |
| Recent activity | Several recent item/task actions by the reviewer account. |

## Manual API Seeding Outline

Use this only from a trusted machine. Replace placeholders locally and do not save tokens in shell history if that environment is shared.

```powershell
$env:CONVY_API_BASE_URL = "https://api.convyapp.com"
$env:FIREBASE_ID_TOKEN = "<paste reviewer Firebase ID token here>"
```

1. Create or confirm the household through the mobile app or API.
2. Create or confirm one shopping list named `Weekly Groceries`.
3. Create or confirm one task list named `Home Tasks`.
4. Add pending and completed sample shopping items.
5. Add pending and completed sample tasks.
6. Trigger a few item/task state changes so `convy_get_recent_activity` has useful output.
7. Sign out and sign back in as `demo@convyapp.com` before connecting ChatGPT.

Prefer the mobile app for seeding if it is available, because it exercises the same user-facing flows the reviewer may inspect.
