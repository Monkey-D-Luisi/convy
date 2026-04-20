package com.convy

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Build
import com.convy.app.di.appModule
import com.convy.shared.config.ApiConfig
import com.convy.shared.data.offline.SyncManager
import com.convy.shared.data.remote.PushTokenProvider
import com.convy.shared.di.sharedModules
import com.convy.app.platform.AppInfoProvider
import com.convy.shared.platform.AndroidFileStorage
import com.convy.shared.platform.AudioRecorder
import com.convy.shared.platform.FileStorage
import com.convy.shared.platform.GoogleSignInHelper
import com.convy.shared.platform.NetworkMonitor
import com.convy.shared.platform.SpeechRecognizer
import org.koin.android.ext.koin.androidContext
import org.koin.core.context.startKoin
import org.koin.dsl.module

class ConvyApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        createNotificationChannels()
        startKoin {
            androidContext(this@ConvyApplication)
            modules(sharedModules + appModule + platformModule)
        }
        // Start background sync for offline action queue
        org.koin.java.KoinJavaComponent.getKoin().get<SyncManager>().start()
    }

    private fun createNotificationChannels() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                "list_updates",
                "List Updates",
                NotificationManager.IMPORTANCE_DEFAULT
            ).apply {
                description = "Notifications when items are added or changed in your lists"
            }
            val manager = getSystemService(NotificationManager::class.java)
            manager.createNotificationChannel(channel)
        }
    }
}

private val platformModule = module {
    single { ApiConfig(BuildConfig.API_PROTOCOL, BuildConfig.API_HOST, BuildConfig.API_PORT) }
    single<AppInfoProvider> { AndroidAppInfoProvider() }
    single<PushTokenProvider> { AndroidPushTokenProvider() }
    single<SpeechRecognizer> { AndroidSpeechRecognizer(androidContext()) }
    single<AudioRecorder> { AndroidAudioRecorder(androidContext()) }
    single<GoogleSignInHelper> { AndroidGoogleSignInHelper(androidContext()) }
    single<NetworkMonitor> { AndroidNetworkMonitor(androidContext()) }
    single<FileStorage> { AndroidFileStorage(androidContext()) }
}
