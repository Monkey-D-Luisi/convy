---
name: firebase-setup
description: "Setup and verification checklist for Firebase Authentication in Convy. Use when implementing auth features, onboarding, or when Firebase configuration needs to be verified."
---
# Firebase Setup Workflow

## MANDATORY RULES — DO NOT SKIP

1. **Never assume Firebase is configured.** Always run the verification checklist (Step 5) before proceeding with any auth feature.
2. **Use questionnaires.** Every piece of information needed from the user (project ID, package name, file placement confirmation) MUST be requested via `vscode_askQuestions`. Never guess values.
3. **Existing project.** Convy's Firebase project is `convy-6520d` with package `com.monkeydluisi.convy`. If this project already exists, verify — don't recreate.
4. **SHA keys.** Debug and release SHA-1/SHA-256 must be registered in Firebase Console for Google Sign-In to work. Guide the user to generate them via `./gradlew.bat signingReport`.

## When to Use
- First-time Firebase project setup for Convy
- Implementing/modifying any authentication feature
- Verifying that Firebase is correctly configured across backend and mobile
- Troubleshooting authentication failures

## Prerequisites
- Google account with access to Firebase Console
- `google-services.json` file (for Android)

## Procedure

### Step 1: Firebase Console Setup
**Ask the user via `vscode_askQuestions`:**
- Do you already have a Firebase project? What is the Project ID?
- Is Authentication enabled? Which sign-in methods?
- What is the Android package name registered in Firebase?

If the project exists (expected: `convy-6520d`):
1. Verify via questionnaire that **Authentication** is enabled with Email/Password + Google.
2. Note the **Project ID** — needed for backend config.

If new setup is needed:
1. Ask the user to go to [Firebase Console](https://console.firebase.google.com/).
2. Create a project named **"Convy"**.
3. Enable **Authentication** → Sign-in method:
   - **Email/Password**: Enable
   - **Google**: Enable (provides `google-services.json` client ID)
4. Ask the user for the **Project ID** via `vscode_askQuestions`.

### Step 2: Backend Configuration
The backend validates Firebase JWT tokens. Configuration is in `backend/src/Convy.API/Program.cs`.

1. **appsettings.json** — Add or verify the Firebase section:
   ```json
   {
     "Firebase": {
       "ProjectId": "your-firebase-project-id"
     }
   }
   ```
2. **appsettings.Development.json** — Same structure with your dev project ID.
3. **Verify** the JWT middleware is wired in `Program.cs`:
   - `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`
   - `AddJwtBearer` with `Authority = https://securetoken.google.com/{projectId}`
   - `ValidIssuer` and `ValidAudience` match the project ID
4. **Environment variables** (production): Set `Firebase__ProjectId` as environment variable.

### Step 3: Mobile Configuration (Android)
**Ask the user via `vscode_askQuestions`:**
- Have you added the Android app in Firebase Console with package `com.monkeydluisi.convy`?
- Have you added the SHA-1 and SHA-256 debug keys? (Guide them to use `./gradlew.bat signingReport`)
- Have you downloaded the `google-services.json` file?

1. Ask the user to place `google-services.json` in `mobile/androidApp/`.
2. **Verify** the file exists via `Test-Path` or `read_file`.
3. **Verify** the `package_name` and `project_id` inside the JSON match the expected values.
4. Verify `mobile/androidApp/build.gradle.kts` has the Google Services plugin:
   ```kotlin
   plugins {
       id("com.google.gms.google-services")
   }
   ```
4. Verify `mobile/build.gradle.kts` (root) has:
   ```kotlin
   id("com.google.gms.google-services") version "..." apply false
   ```
5. Add Firebase Auth dependency in `mobile/shared/build.gradle.kts` (if not present):
   ```kotlin
   implementation("dev.gitlive:firebase-auth:<version>")
   ```

### Step 4: Mobile Auth Integration
1. **AuthRepository** — Verify implementation uses Firebase Auth SDK:
   - `signInWithEmailAndPassword(email, password)`
   - `createUserWithEmailAndPassword(email, password)`
   - `currentUser?.getIdToken(forceRefresh)` for getting JWT
2. **Ktor HTTP client** — Verify auth token interceptor attaches the Firebase ID token:
   ```kotlin
   install(Auth) {
       bearer {
           loadTokens { BearerTokens(authRepository.getIdToken(), "") }
       }
   }
   ```
3. If using a `StubAuthRepository` for development, ensure it returns a valid-looking stub token and is **NOT** used in release builds.

### Step 5: Verification Checklist
- [ ] Firebase project exists and Authentication is enabled
- [ ] `Firebase:ProjectId` is set in backend `appsettings.json`
- [ ] Backend JWT validation middleware is configured in `Program.cs`
- [ ] `google-services.json` is in `mobile/androidApp/`
- [ ] Google Services Gradle plugin is applied
- [ ] Firebase Auth SDK dependency is declared
- [ ] Mobile auth repository implements login/register/token retrieval
- [ ] Ktor client attaches Firebase ID token to API requests
- [ ] Backend endpoints with `[Authorize]` reject requests without valid tokens
- [ ] Backend `ICurrentUserService` extracts the Firebase UID from JWT claims

## Troubleshooting
| Symptom | Cause | Fix |
|---------|-------|-----|
| 401 on all API calls | Missing/invalid Firebase token | Check mobile token attachment; verify `ProjectId` matches |
| Token validation fails | Wrong `ProjectId` in appsettings | Ensure appsettings `Firebase:ProjectId` matches Firebase Console |
| `google-services.json` not found | File not in right directory | Must be in `mobile/androidApp/`, not project root |
| Mobile build fails on Firebase | Missing Gradle plugin | Add `com.google.gms.google-services` plugin to both root and app `build.gradle.kts` |
| `StubAuthRepository` in production | DI misconfiguration | Ensure Koin module uses real `AuthRepository` except in test/debug |
