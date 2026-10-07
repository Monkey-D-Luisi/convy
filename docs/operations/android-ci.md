# Android CI SDK setup

The mobile CI continues to use setup-android v3, Java 17 and the checked-in Gradle wrapper. The action's default packages include the removed SDK `tools` package. An explicit supported package list now requests only platform-tools, Android platform 35 and build-tools 34.0.0, the default required by Android Gradle Plugin 8.7.3.

The applications retain compileSdk/targetSdk 35, their existing flavors, signing and Play/internal release configuration. The runner's existing SDK is usable, but the explicit setup also installs these components if they are absent. No obsolete `tools` package is requested.

CI still executes both unit-test tasks and assembles `androidApp:assembleLocalDebug`. This change does not remove mobile validation or modify production release behavior.

Sources: [setup-android action inputs](https://github.com/android-actions/setup-android/blob/v3/action.yml), [Android Gradle Plugin 8.7 compatibility](https://developer.android.com/build/releases/past-releases/agp-8-7-0-release-notes).
