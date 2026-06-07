# Android Play Internal Publishing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish the signed staging Android App Bundle to Google Play Internal Testing from GitHub Actions without exposing installable app artifacts in this public repository.

**Architecture:** Keep CI validation separate from release publishing. Add a protected Android release workflow that runs only from trusted `master` builds, uses environment secrets, generates the signed AAB, uploads it through the official Google Play Developer API, and does not upload APK/AAB files to GitHub artifacts.

**Tech Stack:** GitHub Actions, Android Gradle Plugin, Google Play Android Publisher API, Google GitHub Actions authentication, Bash, `curl`, `jq`.

---

### Task 1: Add Play Publishing Workflow

**Files:**
- Create: `.github/workflows/android-play-internal.yml`

- [x] **Step 1: Create a trusted-only workflow**

Use `workflow_run` after successful CI on `master` and `workflow_dispatch` for manual re-runs. Reference the `android-release` environment so secrets remain gated by GitHub environment protection.

- [x] **Step 2: Recreate ignored Android secret files at runtime**

Decode base64 environment secrets into `mobile/androidApp/google-services.json`, `mobile/keystore.properties`, and `mobile/keystore/convy-release.keystore`. Never echo secret values.

- [x] **Step 3: Build and upload only the AAB**

Run `./gradlew :androidApp:bundleStagingRelease --console=plain`, then use Google Play edit endpoints to upload the AAB to the `internal` track and commit the edit. Do not call `actions/upload-artifact` for APK or AAB outputs.

### Task 2: Document Secrets and Operations

**Files:**
- Modify: `docs/VERSIONING.md`
- Modify: `docs/DEPLOYMENT.md`

- [x] **Step 1: Document required GitHub environment**

Describe `android-release`, branch restrictions, manual approval, and the exact environment secrets.

- [x] **Step 2: Document the service account permissions**

Describe the Play Console API access requirement and the least-privilege app release role needed to upload bundles to Internal Testing.

- [x] **Step 3: Document artifact policy**

State that installable Android binaries must not be uploaded as public GitHub Actions artifacts while the repository is public.

### Task 3: Verify

**Files:**
- Test: `.github/workflows/android-play-internal.yml`
- Test: `docs/VERSIONING.md`
- Test: `docs/DEPLOYMENT.md`

- [x] **Step 1: Parse workflow YAML**

Run a local YAML parser over `.github/workflows/android-play-internal.yml` and existing workflows.

- [x] **Step 2: Verify Android release build still works**

Run `cd mobile && ./gradlew :androidApp:bundleStagingRelease --console=plain`.

- [x] **Step 3: Review diff for secret exposure**

Search the staged diff for literal credentials and confirm only secret names appear.

## Execution Notes

- `ANDROID_GOOGLE_SERVICES_JSON_B64`, `ANDROID_KEYSTORE_PROPERTIES_B64`, and `ANDROID_RELEASE_KEYSTORE_B64` are configured as `android-release` environment secrets in GitHub.
- `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON_B64` is configured as an `android-release` environment secret in GitHub.
- The dedicated release service account is `convy-play-release@convy-6520d.iam.gserviceaccount.com`. It was created in the `convy-6520d` Google Cloud project and invited in Play Console with app-level access to Convy.
- `android-release` and `staging` GitHub environments are restricted to the `master` deployment branch.
