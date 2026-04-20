# Convy E2E Test Runner
# Generates unique emails per run and passes them as env vars to Maestro
param(
    [string]$MaestroPath = "$env:USERPROFILE\.maestro\maestro\bin\maestro.bat"
)

$ts = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$email = "e2e_${ts}@test.com"
$joinEmail = "e2e_join_${ts}@test.com"
$buildFile = Join-Path (Split-Path $MyInvocation.MyCommand.Path) "..\androidApp\build.gradle.kts"
$buildText = Get-Content -Path $buildFile -Raw
$versionMatch = [regex]::Match($buildText, 'versionName\s*=\s*"([^"]+)"')

if (-not $versionMatch.Success) {
    throw "Unable to read versionName from $buildFile"
}

$appVersion = $versionMatch.Groups[1].Value

Write-Host "Running E2E tests with:"
Write-Host "  EMAIL: $email"
Write-Host "  JOIN_EMAIL: $joinEmail"
Write-Host "  APP_VERSION: $appVersion"
Write-Host ""

Push-Location (Split-Path $MyInvocation.MyCommand.Path)
try {
    $output = & $MaestroPath test -e EMAIL=$email -e JOIN_EMAIL=$joinEmail -e APP_VERSION=$appVersion . 2>&1
    $maestroExitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }

    if ($maestroExitCode -ne 0) {
        exit $maestroExitCode
    }

    $failedFlows = 0
    foreach ($line in $output) {
        if ($line -match "(\d+)/\d+\s+Flows Failed") {
            $failedFlows = [int]$Matches[1]
        }
    }

    if ($failedFlows -gt 0) {
        exit 1
    }
}
finally {
    Pop-Location
}
