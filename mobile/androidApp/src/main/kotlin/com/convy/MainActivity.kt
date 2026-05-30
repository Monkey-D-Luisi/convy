package com.convy

import android.graphics.Color
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.SystemBarStyle
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.convy.app.App
import com.convy.app.ui.demo.UiDemoApp

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val demoRouteId = if (BuildConfig.DEBUG) {
            intent.getStringExtra("convy.demo.route")
        } else {
            null
        }
        enableEdgeToEdge(
            statusBarStyle = SystemBarStyle.auto(Color.TRANSPARENT, Color.TRANSPARENT),
            navigationBarStyle = SystemBarStyle.auto(Color.TRANSPARENT, Color.TRANSPARENT),
        )
        window.isNavigationBarContrastEnforced = false
        setContent {
            if (demoRouteId != null) {
                UiDemoApp(routeId = demoRouteId)
            } else {
                App()
            }
        }
    }
}
