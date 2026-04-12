package com.convy

import android.app.Application
import com.convy.app.di.appModule
import com.convy.shared.config.ApiConfig
import com.convy.shared.data.remote.PushTokenProvider
import com.convy.shared.di.sharedModules
import com.convy.shared.platform.AudioRecorder
import com.convy.shared.platform.GoogleSignInHelper
import com.convy.shared.platform.SpeechRecognizer
import org.koin.android.ext.koin.androidContext
import org.koin.core.context.startKoin
import org.koin.dsl.module

class ConvyApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        startKoin {
            androidContext(this@ConvyApplication)
            modules(sharedModules + appModule + platformModule)
        }
    }
}

private val platformModule = module {
    single { ApiConfig(BuildConfig.API_PROTOCOL, BuildConfig.API_HOST, BuildConfig.API_PORT) }
    single<PushTokenProvider> { AndroidPushTokenProvider() }
    single<SpeechRecognizer> { AndroidSpeechRecognizer(androidContext()) }
    single<AudioRecorder> { AndroidAudioRecorder(androidContext()) }
    single<GoogleSignInHelper> { AndroidGoogleSignInHelper(androidContext()) }
}
