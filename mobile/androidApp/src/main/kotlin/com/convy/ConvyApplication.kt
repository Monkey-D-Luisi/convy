package com.convy

import android.app.Application
import com.convy.app.di.appModule
import com.convy.shared.data.remote.PushTokenProvider
import com.convy.shared.di.sharedModules
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
    single<PushTokenProvider> { AndroidPushTokenProvider() }
    single<SpeechRecognizer> { AndroidSpeechRecognizer(androidContext()) }
}
