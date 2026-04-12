package com.convy

import android.content.Context
import androidx.credentials.CredentialManager
import androidx.credentials.GetCredentialRequest
import com.convy.shared.platform.GoogleSignInHelper
import com.google.android.libraries.identity.googleid.GetGoogleIdOption
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential

class AndroidGoogleSignInHelper(
    private val context: Context,
) : GoogleSignInHelper {

    companion object {
        private const val WEB_CLIENT_ID =
            "863271144614-imh4s3gkfr8p53l6r1vdasop33mo5quo.apps.googleusercontent.com"
    }

    override suspend fun getGoogleIdToken(): String {
        val credentialManager = CredentialManager.create(context)

        val googleIdOption = GetGoogleIdOption.Builder()
            .setFilterByAuthorizedAccounts(false)
            .setServerClientId(WEB_CLIENT_ID)
            .build()

        val request = GetCredentialRequest.Builder()
            .addCredentialOption(googleIdOption)
            .build()

        val result = credentialManager.getCredential(context, request)
        val googleIdTokenCredential = GoogleIdTokenCredential.createFrom(result.credential.data)
        return googleIdTokenCredential.idToken
    }
}
