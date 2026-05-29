# Firebase Auth Checklist For ChatGPT App Review

The agent cannot verify Firebase Console settings without console access. Luis must check this manually before submission.

Path:

```text
Firebase Console -> Authentication -> Settings -> Authorized domains
```

Required domains:

- `auth.convyapp.com`
- `admin.convyapp.com`

Optional only if still used by installed staging builds:

- legacy `nip.io` auth/admin domains

Do not add broad wildcard domains. Remove unused legacy domains after no active reviewer or installed staging build needs them.

## Reviewer Account Checks

- `demo@convyapp.com` exists.
- Email/password sign-in works.
- MFA or additional challenges are disabled for the reviewer account.
- Google Sign-In is not the only sign-in path for the reviewer account.
- The reviewer account can complete OAuth consent at `https://auth.convyapp.com`.
