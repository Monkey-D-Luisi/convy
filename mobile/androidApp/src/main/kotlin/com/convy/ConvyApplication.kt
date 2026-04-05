package com.convy

import android.app.Application
import com.convy.app.di.appModule
import com.convy.shared.di.sharedModules
import org.koin.android.ext.koin.androidContext
import org.koin.core.context.startKoin

class ConvyApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        startKoin {
            androidContext(this@ConvyApplication)
            modules(sharedModules + appModule)
        }
    }
}
