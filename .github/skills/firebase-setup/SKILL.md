---
name: firebase-setup
description: "Setup and verification checklist for Firebase Authentication in Convy. Use when implementing auth features, onboarding, or when Firebase configuration needs to be verified."
---
# Firebase Setup Workflow

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
1. Go to [Firebase Console](https://console.firebase.google.com/).
2. Create a project named **"Convy"** (or reuse an existing one).
3. Enable **Authentication** → Sign-in method:
   - **Email/Password**: Enable
   - **Google**: Enable (provides `google-services.json` client ID)
4. Note the **Project ID** (e.g., `convy-12345`) — needed for backend config.

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
1. In Firebase Console → Project Settings → Add Android App:
   - Package name: `com.convy.app` (check `mobile/androidApp/build.gradle.kts`)
   - Download `google-services.json`
2. Place `google-services.json` in `mobile/androidApp/`.
3. Verify `mobile/androidApp/build.gradle.kts` has the Google Services plugin:
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
