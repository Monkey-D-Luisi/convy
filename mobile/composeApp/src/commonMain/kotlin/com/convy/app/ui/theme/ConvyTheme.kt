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

val ConvyEmerald = Color(0xFF006F4E)
val ConvyEmeraldDeep = Color(0xFF063F32)
val ConvyMint = Color(0xFFB8F3CC)
val ConvyMintSoft = Color(0xFFE7F7EC)
val ConvyLavender = Color(0xFFF1ECF9)
val ConvyLavenderDeep = Color(0xFF5B438A)
val ConvyWarmWhite = Color(0xFFFFFCF8)
val ConvyInk = Color(0xFF17201D)
val ConvySlate = Color(0xFF65716D)
val ConvyLine = Color(0xFFD9DED8)

private val ConvyLightColorScheme = lightColorScheme(
    primary = ConvyEmerald,
    onPrimary = Color.White,
    primaryContainer = ConvyMint,
    onPrimaryContainer = ConvyEmeraldDeep,
    secondary = ConvyLavenderDeep,
    onSecondary = Color.White,
    secondaryContainer = ConvyLavender,
    onSecondaryContainer = ConvyLavenderDeep,
    tertiary = Color(0xFF2F6C72),
    onTertiary = Color.White,
    tertiaryContainer = Color(0xFFDDF5F0),
    onTertiaryContainer = Color(0xFF0C4E47),
    error = Color(0xFFB3261E),
    onError = Color.White,
    errorContainer = Color(0xFFFFDAD6),
    onErrorContainer = Color(0xFF410002),
    background = ConvyWarmWhite,
    onBackground = ConvyInk,
    surface = Color.White,
    onSurface = ConvyInk,
    surfaceVariant = ConvyLavender,
    onSurfaceVariant = ConvySlate,
    outline = Color(0xFF8C9691),
    outlineVariant = ConvyLine,
    inverseSurface = ConvyInk,
    inverseOnSurface = ConvyWarmWhite,
    inversePrimary = ConvyMint,
    surfaceTint = ConvyEmerald,
    surfaceContainerLowest = Color.White,
    surfaceContainerLow = Color(0xFFFBFAF7),
    surfaceContainer = Color(0xFFF6F3F8),
    surfaceContainerHigh = Color(0xFFF0ECF4),
    surfaceContainerHighest = Color(0xFFE7F2EA),
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
    small = RoundedCornerShape(8.dp),
    medium = RoundedCornerShape(8.dp),
    large = RoundedCornerShape(12.dp),
    extraLarge = RoundedCornerShape(24.dp),
)

private val ConvyTypography = Typography(
    displayMedium = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 44.sp,
        lineHeight = 48.sp,
        letterSpacing = 0.sp,
    ),
    headlineLarge = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 32.sp,
        lineHeight = 38.sp,
        letterSpacing = 0.sp,
    ),
    headlineMedium = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 28.sp,
        lineHeight = 34.sp,
        letterSpacing = 0.sp,
    ),
    titleLarge = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 22.sp,
        lineHeight = 28.sp,
        letterSpacing = 0.sp,
    ),
    titleMedium = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.SemiBold,
        fontSize = 18.sp,
        lineHeight = 24.sp,
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
        fontSize = 17.sp,
        lineHeight = 24.sp,
        letterSpacing = 0.sp,
    ),
    bodyMedium = TextStyle(
        fontFamily = FontFamily.Default,
        fontWeight = FontWeight.Normal,
        fontSize = 15.sp,
        lineHeight = 21.sp,
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
        fontSize = 15.sp,
        lineHeight = 20.sp,
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
