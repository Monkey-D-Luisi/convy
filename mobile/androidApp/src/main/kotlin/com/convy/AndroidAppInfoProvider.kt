package com.convy

import com.convy.app.platform.AppInfoProvider

class AndroidAppInfoProvider : AppInfoProvider {
    override val versionName: String = BuildConfig.VERSION_NAME
}
