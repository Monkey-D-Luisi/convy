package com.convy

import com.convy.shared.platform.LocaleProvider
import java.util.Locale

class AndroidLocaleProvider : LocaleProvider {
    override fun getLanguageTag(): String = Locale.getDefault().toLanguageTag()
}
