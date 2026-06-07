# Android Play Internal Release Runbook

This runbook describes how Convy publishes the signed Android App Bundle to Google Play Internal Testing from GitHub Actions.

## Release Channel

The automated release channel is Google Play Internal Testing for:

```text
applicationId = com.monkeydluisi.convy
track = internal
```

Do not upload installable Android binaries as GitHub Actions artifacts while this repository is public. Workflow artifacts in a public repository are not an appropriate access-control boundary for a staging app that points at the controlled-release backend.

## Workflow

Workflow file:

```text
.github/workflows/android-play-internal.yml
```

Triggers:

- Automatically after the `Continuous Integration` workflow succeeds on a `push` to `master`.
- Manually through `workflow_dispatch`, publishing the current `master` ref.

The workflow:

1. Checks out the exact CI commit or current `master` ref for manual runs.
2. Restores ignored Android secret files from GitHub environment secrets.
3. Runs `./gradlew :androidApp:bundleStagingRelease --console=plain`.
4. Uploads `mobile/androidApp/build/outputs/bundle/stagingRelease/androidApp-staging-release.aab` to Google Play Internal Testing through the Google Play Developer API.
5. Deletes restored secret files from the runner.

The workflow intentionally does not call `actions/upload-artifact` for APK or AAB files.

## GitHub Environment

Create a GitHub environment named:

```text
android-release
```

Recommended environment protection:

- Required reviewer: the repository owner.
- Deployment branches: selected branches only, `master`.
- Disable administrator bypass if available for the account plan.

Secrets must be environment secrets, not plain repository secrets, so the publish job cannot read them before environment protection passes.

## Required Environment Secrets

| Secret | Source file | Description |
| --- | --- | --- |
| `ANDROID_GOOGLE_SERVICES_JSON_B64` | `mobile/androidApp/google-services.json` | Firebase Android configuration for release builds. |
| `ANDROID_KEYSTORE_PROPERTIES_B64` | `mobile/keystore.properties` | Gradle signing properties. |
| `ANDROID_RELEASE_KEYSTORE_B64` | `mobile/keystore/convy-release.keystore` | Android release signing keystore. |
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_B64` | Google Play service account JSON key | Service account used only to upload releases to Play. |

Encode a secret from PowerShell:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("mobile/androidApp/google-services.json")) | Set-Clipboard
```

Encode a secret from Bash:

```bash
base64 -w0 mobile/androidApp/google-services.json
```

Repeat for each source file and paste the output into the matching GitHub environment secret.

## Optional Environment Variables

| Variable | Default | Purpose |
| --- | --- | --- |
| `GOOGLE_PLAY_PACKAGE_NAME` | `com.monkeydluisi.convy` | Google Play package name. |
| `GOOGLE_PLAY_TRACK` | `internal` | Target Play track. Keep `internal` for this flow. |
| `GOOGLE_PLAY_RELEASE_STATUS` | `completed` | Release status sent to the track update API. |

Use environment variables for non-secret release configuration. Do not place these values in repository secrets unless they become sensitive.

## Google Play Service Account

Create or reuse a Google Cloud service account connected to Play Console API access.

Current automation service account:

```text
convy-play-release@convy-6520d.iam.gserviceaccount.com
```

Required setup:

1. In Play Console, open **Setup > API access** and link the Google Cloud project used for release automation.
2. Create a service account or select an existing release automation account.
3. Grant app-level access only to Convy.
4. Grant the minimum Play Console permissions needed to create releases on testing tracks.
5. Create a JSON key for that service account.
6. Base64 encode the JSON key and store it as `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_B64` in the `android-release` environment.

Treat the service account JSON key like a password. Rotate it immediately if it is ever copied into logs, issues, pull requests, chat, or committed files.

## Release Procedure

Normal path:

1. Bump `versionCode` and `versionName` in `mobile/androidApp/build.gradle.kts`.
2. Add a matching row to `docs/VERSIONING.md`.
3. Merge to `master` after CI passes.
4. The `Android Play Internal Release` workflow runs after CI succeeds.
5. Approve the `android-release` environment deployment when GitHub prompts for review.
6. Confirm the workflow logs show the expected package, version name, version code, and track.
7. Open Play Console and confirm the new release appears in Internal Testing.

Manual re-run:

1. Open GitHub Actions.
2. Select `Android Play Internal Release`.
3. Approve the `android-release` environment deployment.

## Verification

After a successful publish:

```text
Package: com.monkeydluisi.convy
Track: internal
versionCode: latest row in docs/VERSIONING.md
versionName: latest row in docs/VERSIONING.md
```

Google Play serves the highest compatible version code available on a track the tester is eligible to receive. If a tester does not see the update, confirm:

- The tester is in the Internal Testing email list.
- The tester has accepted the opt-in link.
- The new release is available on the internal track.
- The tester's device is compatible with the app bundle.

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `Missing required android-release environment secret` | Secret not configured or configured in repository scope instead of environment scope. | Add the secret to the `android-release` environment. |
| Gradle signing failure | `keystore.properties` or keystore secret is wrong. | Re-encode the local release signing files and update environment secrets. |
| `processStagingReleaseGoogleServices` fails | Firebase config secret is missing or malformed. | Re-encode `mobile/androidApp/google-services.json`. |
| Google Play returns 401 or 403 | Service account cannot access the app or lacks release permissions. | Review Play Console API access and app-level permissions. |
| Uploaded version code mismatch | Build file and generated bundle disagree. | Regenerate the bundle from the checked-out ref and verify `mobile/androidApp/build.gradle.kts`. |
| Release already has this version code | `versionCode` was reused. | Increment `versionCode`, update `docs/VERSIONING.md`, and publish again. |
