param(
    [string] $TerraformDirectory,
    [string] $TokenPath,

    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]] $TerraformArguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $TerraformArguments -or $TerraformArguments.Count -eq 0) {
    $TerraformArguments = @("plan")
}

if (-not $TerraformDirectory) {
    $TerraformDirectory = Join-Path $PSScriptRoot "..\..\infra\hetzner"
}

if (-not $TokenPath) {
    $defaultTokenPath = "$env:USERPROFILE\.config\convy\secrets\hcloud-token.txt"
    $legacyTokenPath = "$env:USERPROFILE\secrets\hetzner"
    $TokenPath = if (Test-Path -LiteralPath $defaultTokenPath) { $defaultTokenPath } else { $legacyTokenPath }
}

if (-not (Test-Path -LiteralPath $TokenPath)) {
    throw "Hetzner token file not found at $TokenPath. Create it outside the repo and do not commit it."
}

$token = (Get-Content -Raw -LiteralPath $TokenPath).Trim()
if ([string]::IsNullOrWhiteSpace($token)) {
    throw "Hetzner token file is empty: $TokenPath"
}

$env:HCLOUD_TOKEN = $token

Push-Location -LiteralPath $TerraformDirectory
try {
    terraform @TerraformArguments
    exit $LASTEXITCODE
}
finally {
    Pop-Location
    Remove-Item Env:\HCLOUD_TOKEN -ErrorAction SilentlyContinue
}
