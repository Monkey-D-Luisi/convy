# V1 QA Checklist

Use this checklist before promoting a private internal V1 build.

## Version Consistency

- Confirm `mobile/androidApp/build.gradle.kts` has the intended `versionCode` and `versionName`.
- Install the build on an emulator and confirm `adb shell dumpsys package com.monkeydluisi.convy` reports the same `versionName`.
- Open Settings and confirm the displayed app version matches the installed package.
- Run the Settings E2E flow with `APP_VERSION` from `mobile/e2e/run-e2e.ps1`.

## Sharing And Invitations

- User A creates a household and opens Members.
- User A generates an invite, copies it, sees it under Active invites, copies it again from Active invites, and revokes it.
- User B registers, joins with a still-active copied code, lands in User A's household, and sees the same household name.
- User B cannot join with a revoked, expired, or malformed code; the app stays recoverable.

## Voice Item Entry

- Grant microphone permission and record a normal multi-item grocery phrase.
- Confirm the transcript sheet appears, parsed items can be selected or deselected, and batch add creates the selected items.
- Deny microphone permission and confirm the app explains that permission is required.
- Stop a recording too quickly and confirm the short-recording error appears.
- Test empty/noisy audio and backend parse failure; the user should stay on the list with a recoverable error.

## Realtime And Offline Reliability

- Run two clients signed into different users in the same household.
- Verify list create, item add, edit, complete, uncomplete, delete, list rename, list archive, household rename, invite join, and invite revoke propagate or refresh correctly.
- Disconnect the backend or network during common item actions; queued changes should show pending sync and recover after reconnect.
- Backend-down startup must show a retryable error instead of an indefinite spinner.

## Daily Use

- Add common items by title only, by suggestion, by duplicate warning dismissal, and by voice batch.
- In shopping mode, confirm filters/search/FAB/voice controls are hidden, progress stays visible, and one-tap completion works.
- Review Activity after shared actions and confirm actor, action, item/list context, and time are understandable.
