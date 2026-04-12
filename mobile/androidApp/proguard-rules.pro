# Add project specific ProGuard rules here.

# =====================
# Kotlin Serialization
# =====================
-keepattributes *Annotation*, InnerClasses
-dontnote kotlinx.serialization.AnnotationsKt
-keepclassmembers class kotlinx.serialization.json.** { *** Companion; }
-keepclasseswithmembers class kotlinx.serialization.json.** { kotlinx.serialization.KSerializer serializer(...); }
-keep,includedescriptorclasses class com.convy.shared.data.remote.dto.**$$serializer { *; }
-keepclassmembers class com.convy.shared.data.remote.dto.** { *** Companion; }
-keepclasseswithmembers class com.convy.shared.data.remote.dto.** { kotlinx.serialization.KSerializer serializer(...); }

# =====================
# Ktor
# =====================
-keep class io.ktor.** { *; }
-keepclassmembers class io.ktor.** { volatile <fields>; }
-keep class io.ktor.client.engine.** { *; }
-dontwarn io.ktor.**

# =====================
# Koin
# =====================
-keep class org.koin.** { *; }
-dontwarn org.koin.**

# =====================
# Firebase
# =====================
-keep class com.google.firebase.** { *; }
-dontwarn com.google.firebase.**
-keep class dev.gitlive.firebase.** { *; }
-dontwarn dev.gitlive.firebase.**

# =====================
# Coroutines
# =====================
-keepnames class kotlinx.coroutines.internal.MainDispatcherFactory {}
-keepnames class kotlinx.coroutines.CoroutineExceptionHandler {}
-keepclassmembers class kotlinx.coroutines.** { volatile <fields>; }

# =====================
# Kotlin Metadata
# =====================
-keep class kotlin.Metadata { *; }
-keepclassmembers class kotlin.Metadata { public <methods>; }

# =====================
# Compose
# =====================
-dontwarn androidx.compose.**

# =====================
# General
# =====================
-keepattributes SourceFile,LineNumberTable
-renamesourcefileattribute SourceFile
