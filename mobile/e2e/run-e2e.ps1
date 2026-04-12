# Convy E2E Test Runner
# Generates unique emails per run and passes them as env vars to Maestro
param(
    [string]$MaestroPath = "$env:USERPROFILE\.maestro\maestro\bin\maestro.bat"
)

$ts = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$email = "e2e_${ts}@test.com"
$joinEmail = "e2e_join_${ts}@test.com"

Write-Host "Running E2E tests with:"
Write-Host "  EMAIL: $email"
Write-Host "  JOIN_EMAIL: $joinEmail"
Write-Host ""

Push-Location (Split-Path $MyInvocation.MyCommand.Path)
& $MaestroPath test -e EMAIL=$email -e JOIN_EMAIL=$joinEmail .
Pop-Location
