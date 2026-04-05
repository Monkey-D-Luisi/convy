package com.convy.app.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Shapes
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.ui.unit.dp

// Convy "Hearth" design system — generated via Stitch (Gemini 3.1 Pro)
// Primary: Teal/Emerald  Secondary: Muted green  Tertiary: Deep teal

private val ConvyLightColorScheme = lightColorScheme(
    primary = Color(0xFF006D4A),
    onPrimary = Color(0xFFE6FFEE),
    primaryContainer = Color(0xFF69F6B8),
    onPrimaryContainer = Color(0xFF005A3C),
    secondary = Color(0xFF4E6457),
    onSecondary = Color(0xFFE6FFEE),
    secondaryContainer = Color(0xFFD0E8D8),
    onSecondaryContainer = Color(0xFF41574A),
    tertiary = Color(0xFF2D676D),
    onTertiary = Color(0xFFE8FDFF),
    tertiaryContainer = Color(0xFFB2EDF3),
    onTertiaryContainer = Color(0xFF1C5A5F),
    error = Color(0xFFA83836),
    onError = Color(0xFFFFF7F6),
    errorContainer = Color(0xFFFA746F),
    onErrorContainer = Color(0xFF6E0A12),
    background = Color(0xFFF8F9FA),
    onBackground = Color(0xFF2D3335),
    surface = Color(0xFFF8F9FA),
    onSurface = Color(0xFF2D3335),
    surfaceVariant = Color(0xFFDEE3E6),
    onSurfaceVariant = Color(0xFF5A6062),
    outline = Color(0xFF767C7E),
    outlineVariant = Color(0xFFADB3B5),
    inverseSurface = Color(0xFF0C0F10),
    inverseOnSurface = Color(0xFF9B9D9E),
    inversePrimary = Color(0xFF69F6B8),
    surfaceTint = Color(0xFF006D4A),
)

private val ConvyDarkColorScheme = darkColorScheme(
    primary = Color(0xFF69F6B8),
    onPrimary = Color(0xFF003825),
    primaryContainer = Color(0xFF005236),
    onPrimaryContainer = Color(0xFF69F6B8),
    secondary = Color(0xFFC2DACA),
    onSecondary = Color(0xFF2E4438),
    secondaryContainer = Color(0xFF4A6054),
    onSecondaryContainer = Color(0xFFD0E8D8),
    tertiary = Color(0xFFA5DEE4),
    onTertiary = Color(0xFF00474C),
    tertiaryContainer = Color(0xFF296469),
    onTertiaryContainer = Color(0xFFB2EDF3),
    error = Color(0xFFFFB4AB),
    onError = Color(0xFF690005),
    errorContainer = Color(0xFF93000A),
    onErrorContainer = Color(0xFFFFDAD6),
    background = Color(0xFF0F1311),
    onBackground = Color(0xFFDFE2EF),
    surface = Color(0xFF0F1311),
    onSurface = Color(0xFFDFE2EF),
    surfaceVariant = Color(0xFF31353E),
    onSurfaceVariant = Color(0xFFC7C4D7),
    outline = Color(0xFF908FA0),
    outlineVariant = Color(0xFF464554),
    inverseSurface = Color(0xFFDFE2EF),
    inverseOnSurface = Color(0xFF2C303A),
    inversePrimary = Color(0xFF006D4A),
    surfaceTint = Color(0xFF69F6B8),
)

private val ConvyShapes = Shapes(
    small = RoundedCornerShape(8.dp),
    medium = RoundedCornerShape(12.dp),
    large = RoundedCornerShape(16.dp),
    extraLarge = RoundedCornerShape(28.dp),
)

@Composable
fun ConvyTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit,
) {
    val colorScheme = if (darkTheme) ConvyDarkColorScheme else ConvyLightColorScheme

    MaterialTheme(
        colorScheme = colorScheme,
        shapes = ConvyShapes,
        content = content,
    )
}
