# Mobile App Versioning

## Rules

- **`versionCode`** is a strictly increasing integer. Play Store rejects any APK/AAB whose code is equal to or lower than the latest code already uploaded to the target track. Never reuse a code.
- **`versionName`** is the human-readable semantic version. It must reflect the real app state shown to testers.
- Both values live in [`mobile/androidApp/build.gradle.kts`](../mobile/androidApp/build.gradle.kts) under `defaultConfig`.
- The Settings screen must read the same build version at runtime; it must not hardcode a version string.
- The E2E runner passes the current `versionName` as `APP_VERSION`, and the Settings flow validates that value.

## When To Increment

| Change | `versionCode` | `versionName` |
|--------|---------------|---------------|
| Hotfix or rejected re-upload | +1 | unchanged |
| Feature iteration | +1 | `PATCH+1` |
| Minor release | +1 | `MINOR+1`, reset patch |
| Major release | +1 | `MAJOR+1`, reset minor and patch |

`versionCode` always increases by 1, regardless of the type of release.

## History

| versionCode | versionName | Date | Notes |
|-------------|-------------|------|-------|
| 1 | 0.1.0 | - | First internal upload |
| 2 | 0.1.1 | - | Internal testing iteration |
| 3 | 0.1.2 | - | Internal testing iteration |
| 4 | 0.1.3 | 2026-04-12 | Rejected by Play Store because the code had already been used |
| 5 | 0.1.3 | 2026-04-12 | Re-upload with EN and ES i18n |
| 6 | 0.1.4 | 2026-04-17 | Internal testing iteration |
| 7 | 0.1.4 | 2026-04-17 | V1 hardening baseline |
| 8 | 0.1.5 | 2026-04-20 | Internal hardening validation build |
| 9 | 0.1.6 | 2026-04-20 | PR review follow-up hardening build |

## Release Procedure

1. Confirm the latest used `versionCode` in this file and in Play Console.
2. Increment `versionCode` by 1 in `mobile/androidApp/build.gradle.kts`.
3. Update `versionName` according to the table above.
4. Add a row to the history table.
5. Build the release bundle:
   ```powershell
   cd mobile
   .\gradlew :androidApp:bundleStagingRelease
   ```
6. Upload the generated AAB from:
   ```text
   mobile/androidApp/build/outputs/bundle/stagingRelease/androidApp-staging-release.aab
   ```
7. Commit with a conventional message such as `chore: bump versionCode to X (vN.N.N)`.
