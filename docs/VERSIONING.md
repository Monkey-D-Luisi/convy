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
| 10 | 0.1.7 | 2026-04-26 | Mobile list interaction improvements |
| 11 | 0.1.8 | 2026-04-26 | Mobile list interaction review follow-up |
| 12 | 0.1.9 | 2026-04-26 | Notification preferences and localization |
| 13 | 0.1.10 | 2026-05-08 | Low-cost VPS production hosting path |
| 14 | 0.1.11 | 2026-05-17 | Shopping item state metadata |
| 15 | 0.1.12 | 2026-05-17 | Android VPS host hotfix |
| 16 | 0.1.13 | 2026-05-18 | Operability metrics and admin dashboard |
| 17 | 0.1.14 | 2026-05-19 | Admin operability follow-ups |
| 18 | 0.1.15 | 2026-05-22 | Landing package link correction and internal test artifacts |
| 19 | 0.1.16 | 2026-05-22 | UI/UX quality and admin hardening |
| 20 | 0.1.17 | 2026-05-23 | Multi-household management and lower voice action UX |
| 21 | 0.1.18 | 2026-05-29 | Master audit hardening and staging validation build |
| 22 | 0.1.19 | 2026-05-29 | Mobile app and landing redesign |
| 23 | 0.1.20 | 2026-05-29 | Post-redesign release artifact build |
| 24 | 0.1.21 | 2026-05-29 | Landing copy and list detail bug fixes |

## Release Procedure

1. Confirm the latest used `versionCode` in this file and in Play Console.
2. Increment `versionCode` by 1 in `mobile/androidApp/build.gradle.kts`.
3. Update `versionName` according to the table above.
4. Add a row to the history table.
5. Confirm local-only mobile files exist. They are intentionally git-ignored and must be present in every worktree used to generate Android artifacts:
   ```text
   mobile/local.properties
   mobile/androidApp/google-services.json
   mobile/keystore.properties
   mobile/keystore/convy-release.keystore
   ```
   If a generated worktree is missing them, copy them from the canonical local checkout before building. Do not commit these files.
6. Build the local debug APK, signed staging APK, and signed staging AAB:
   ```powershell
   cd mobile
   .\gradlew :androidApp:assembleLocalDebug
   .\gradlew :androidApp:assembleStagingRelease
   .\gradlew :androidApp:bundleStagingRelease
   ```
7. Use the standard Gradle output paths. Do not copy release artifacts into ad hoc zip files or custom top-level artifact directories:
   ```text
   mobile/androidApp/build/outputs/apk/local/debug/androidApp-local-debug.apk
   mobile/androidApp/build/outputs/apk/staging/release/androidApp-staging-release.apk
   mobile/androidApp/build/outputs/bundle/stagingRelease/androidApp-staging-release.aab
   ```
8. If QA screenshots are captured, keep them in standard build output folders:
   ```text
   mobile/build/outputs/qa/<scenario>/
   docs/build/outputs/qa/
   ```
9. Upload the generated AAB from:
   ```text
   mobile/androidApp/build/outputs/bundle/stagingRelease/androidApp-staging-release.aab
   ```
10. Commit with a conventional message such as `chore: bump versionCode to X (vN.N.N)`.

## Artifact Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `processLocalDebugGoogleServices` or `processStagingReleaseGoogleServices` fails | Missing `mobile/androidApp/google-services.json` | Copy the ignored Firebase config into `mobile/androidApp/` |
| `signStagingReleaseBundle` or `packageStagingRelease` fails | Missing `mobile/keystore.properties` or `mobile/keystore/convy-release.keystore` | Copy both ignored signing files into the worktree |
| Only an intermediary unsigned AAB exists under `build/intermediates` | Signing did not complete | Fix signing files and rerun `.\gradlew :androidApp:bundleStagingRelease`; use only `build/outputs/bundle/stagingRelease/androidApp-staging-release.aab` |
| Release artifacts were copied into custom directories | Non-standard local packaging | Regenerate them with Gradle and reference the standard `mobile/androidApp/build/outputs/...` paths |
