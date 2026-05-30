package com.convy.app.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Shapes
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

val ConvyEmerald = Color(0xFF006B55)
val ConvyEmeraldDeep = Color(0xFF083B31)
val ConvyMint = Color(0xFFC5EBDD)
val ConvyMintSoft = Color(0xFFEFF8F3)
val ConvyLavender = Color(0xFFE6DFF0)
val ConvyLavenderDeep = Color(0xFF5A5278)
val ConvyWarmWhite = Color(0xFFFCFAF6)
val ConvyInk = Color(0xFF17211D)
val ConvySlate = Color(0xFF5E6A64)
val ConvyLine = Color(0xFFD8DED7)

private val ConvyLightColorScheme = lightColorScheme(
    primary = ConvyEmerald,
    onPrimary = Color.White,
    primaryContainer = ConvyMint,
    onPrimaryContainer = ConvyEmeraldDeep,
    secondary = ConvyLavenderDeep,
    onSecondary = Color.White,
    secondaryContainer = ConvyLavender,
    onSecondaryContainer = ConvyLavenderDeep,
    tertiary = Color(0xFF6F5A32),
    onTertiary = Color.White,
    tertiaryContainer = Color(0xFFFFE3B0),
    onTertiaryContainer = Color(0xFF4C390B),
    error = Color(0xFFB3261E),
    onError = Color.White,
    errorContainer = Color(0xFFFFDAD6),
    onErrorContainer = Color(0xFF410002),
    background = ConvyWarmWhite,
    onBackground = ConvyInk,
    surface = Color.White,
    onSurface = ConvyInk,
    surfaceVariant = Color(0xFFE1E7E0),
    onSurfaceVariant = ConvySlate,
    outline = Color(0xFF8C9691),
    outlineVariant = ConvyLine,
    inverseSurface = ConvyInk,
    inverseOnSurface = ConvyWarmWhite,
    inversePrimary = ConvyMint,
    surfaceTint = ConvyEmerald,
    surfaceContainerLowest = Color.White,
    surfaceContainerLow = Color(0xFFF8F6F1),
    surfaceContainer = Color(0xFFF1F0EA),
    surfaceContainerHigh = Color(0xFFECE9E2),
    surfaceContainerHighest = Color(0xFFE1E7E0),
)

private val ConvyDarkColorScheme = darkColorScheme(
    primary = ConvyMint,
    onPrimary = ConvyEmeraldDeep,
    primaryContainer = ConvyEmeraldDeep,
    onPrimaryContainer = ConvyMint,
    secondary = Color(0xFFDCCAFF),
    onSecondary = Color(0xFF2B174A),
    secondaryContainer = Color(0xFF44305F),
    onSecondaryContainer = ConvyLavender,
    tertiary = Color(0xFFA9DDD8),
    onTertiary = Color(0xFF123D39),
    tertiaryContainer = Color(0xFF24534E),
    onTertiaryContainer = Color(0xFFDDF5F0),
    error = Color(0xFFFFB4AB),
    onError = Color(0xFF690005),
    errorContainer = Color(0xFF93000A),
    onErrorContainer = Color(0xFFFFDAD6),
    background = Color(0xFF101512),
    onBackground = Color(0xFFE2E8E3),
    surface = Color(0xFF171D19),
    onSurface = Color(0xFFE2E8E3),
    surfaceVariant = Color(0xFF303830),
    onSurfaceVariant = Color(0xFFC2CBC3),
    outline = Color(0xFF8B958D),
    outlineVariant = Color(0xFF414A43),
    inverseSurface = Color(0xFFE2E8E3),
    inverseOnSurface = Color(0xFF202720),
    inversePrimary = ConvyEmerald,
    surfaceTint = ConvyMint,
)

private val ConvyShapes = Shapes(
    small = RoundedCornerShape(6.dp),
    medium = RoundedCornerShape(8.dp),
    large = RoundedCornerShape(12.dp),
    extraLarge = RoundedCornerShape(20.dp),
)

private val ConvyTypography = Typography(
    displayMedium = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 40.sp,
        lineHeight = 44.sp,
        letterSpacing = 0.sp,
    ),
    headlineLarge = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 30.sp,
        lineHeight = 36.sp,
        letterSpacing = 0.sp,
    ),
    headlineMedium = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 25.sp,
        lineHeight = 31.sp,
        letterSpacing = 0.sp,
    ),
    titleLarge = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 20.sp,
        lineHeight = 26.sp,
        letterSpacing = 0.sp,
    ),
    titleMedium = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 16.sp,
        lineHeight = 22.sp,
        letterSpacing = 0.sp,
    ),
    titleSmall = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 15.sp,
        lineHeight = 20.sp,
        letterSpacing = 0.sp,
    ),
    bodyLarge = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.Normal,
        fontSize = 16.sp,
        lineHeight = 23.sp,
        letterSpacing = 0.sp,
    ),
    bodyMedium = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.Normal,
        fontSize = 14.sp,
        lineHeight = 20.sp,
        letterSpacing = 0.sp,
    ),
    bodySmall = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.Normal,
        fontSize = 13.sp,
        lineHeight = 18.sp,
        letterSpacing = 0.sp,
    ),
    labelLarge = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 14.sp,
        lineHeight = 19.sp,
        letterSpacing = 0.sp,
    ),
    labelMedium = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 13.sp,
        lineHeight = 18.sp,
        letterSpacing = 0.sp,
    ),
    labelSmall = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.Medium,
        fontSize = 12.sp,
        lineHeight = 16.sp,
        letterSpacing = 0.sp,
    ),
)

@Composable
fun ConvyTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit,
) {
    val colorScheme = if (darkTheme) ConvyDarkColorScheme else ConvyLightColorScheme

    MaterialTheme(
        colorScheme = colorScheme,
        typography = ConvyTypography,
        shapes = ConvyShapes,
        content = content,
    )
}
