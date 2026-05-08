param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $TerraformArguments = @("plan"),

    [string] $TerraformDirectory = (Join-Path $PSScriptRoot "..\..\infra\hetzner"),
    [string] $TokenPath = "$env:USERPROFILE\.config\convy\secrets\hcloud-token.txt"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $TerraformArguments -or $TerraformArguments.Count -eq 0) {
    $TerraformArguments = @("plan")
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
