# Submission Screenshot Checklist

Store screenshots outside source control until they have been reviewed for personal data. Do not include real user household names, emails, tokens, or private list content.

Suggested local folder:

```text
submission-assets/
  01-chatgpt-connect-convy.png
  02-oauth-consent.png
  03-households-context.png
  04-shopping-list-read.png
  05-write-confirmation.png
  06-items-added-result.png
  07-widget-summary.png
  08-mobile-list-updated.png
  09-negative-case-no-tool.png
```

## Required Screenshots

- App connection screen in ChatGPT Developer Mode.
- OAuth consent screen at `https://auth.convyapp.com`.
- ChatGPT showing Convy households.
- ChatGPT showing the `Weekly Groceries` shopping list.
- ChatGPT asking for confirmation before a write.
- ChatGPT confirming shopping items or tasks were added.
- React Apps SDK widget rendering the Convy summary.

## Optional Screenshots

- Mobile app or Convy data showing the items/tasks after write execution.
- ChatGPT handling a destructive negative prompt without invoking a Convy tool.
- ChatGPT showing recent Convy activity.

## Review Before Upload

- The reviewer account email is acceptable to show only if it is the demo account.
- No passwords, tokens, real addresses, or personal household content are visible.
- Browser address bars show `convyapp.com`, `auth.convyapp.com`, `mcp.convyapp.com`, or ChatGPT only.
- The screenshots correspond to the deployed commit that will be submitted.
