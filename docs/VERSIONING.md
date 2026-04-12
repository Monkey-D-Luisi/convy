# Mobile App Versioning

## Rules

- **`versionCode`** — Entero estrictamente creciente. Play Store rechaza cualquier APK/AAB con un `versionCode` igual o inferior al último publicado en ese track. **Nunca reutilizar un código.**
- **`versionName`** — String legible (semver `MAJOR.MINOR.PATCH`). No tiene restricciones técnicas de Play Store, pero debe reflejar el estado real de la versión.
- Ambos valores viven en [`mobile/androidApp/build.gradle.kts`](../mobile/androidApp/build.gradle.kts) en `defaultConfig`.

## Cuándo incrementar

| Cambio | `versionCode` | `versionName` |
|--------|--------------|--------------|
| Hotfix / re-subida rechazada | +1 | igual |
| Nueva versión de features | +1 | `PATCH+1` |
| Release menor | +1 | `MINOR+1`, reset PATCH |
| Release mayor | +1 | `MAJOR+1`, reset MINOR y PATCH |

`versionCode` **siempre sube en 1** independientemente del tipo de cambio.

## Historial

| versionCode | versionName | Fecha | Notas |
|-------------|-------------|-------|-------|
| 1 | 0.1.0 | — | Primera subida interna |
| 2 | 0.1.1 | — | — |
| 3 | 0.1.2 | — | — |
| 4 | 0.1.3 | 2026-04-12 | ❌ Rechazado por Play Store (código ya usado) |
| 5 | 0.1.3 | 2026-04-12 | Re-subida con i18n (EN + ES) |

## Procedimiento de release

1. Consultar esta tabla para confirmar cuál es el último `versionCode` usado.
2. Incrementar `versionCode` en `+1` en `build.gradle.kts`.
3. Actualizar `versionName` según la tabla anterior.
4. Añadir fila en la tabla **Historial** con fecha y notas.
5. Generar el bundle:
   ```powershell
   cd mobile
   .\gradlew :androidApp:bundleRelease
   ```
6. El AAB de producción queda en:
   ```
   mobile/androidApp/build/outputs/bundle/stagingRelease/androidApp-staging-release.aab
   ```
7. Commit con mensaje: `chore: bump versionCode to X (vN.N.N)`
