[CmdletBinding()]
param(
    [string]$TerraformDirectory,
    [string]$CompartmentOcid,
    [string]$SshPublicKeyPath,
    [double]$InstanceOcpus = 1,
    [double]$InstanceMemoryGb = 6
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-TenancyOcidFromOciConfig {
    $ociConfigPath = Join-Path $env:USERPROFILE ".oci\config"

    if (-not (Test-Path -LiteralPath $ociConfigPath)) {
        throw "OCI config not found at $ociConfigPath. Pass -CompartmentOcid explicitly."
    }

    $tenancyLine = Get-Content -LiteralPath $ociConfigPath |
        Where-Object { $_ -match "^tenancy=" } |
        Select-Object -First 1

    if (-not $tenancyLine) {
        throw "Missing tenancy= in $ociConfigPath. Pass -CompartmentOcid explicitly."
    }

    return $tenancyLine.Substring("tenancy=".Length).Trim()
}

function Invoke-Terraform {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Push-Location -LiteralPath $TerraformDirectory
    try {
        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try {
            $output = & terraform @Arguments 2>&1
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }

        return [pscustomobject]@{
            ExitCode = $exitCode
            Output   = ($output -join "`n")
        }
    }
    finally {
        Pop-Location
    }
}

function Write-RelevantTerraformOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Output
    )

    $Output -split "`n" |
        Where-Object {
            $_ -match "Error:" -or
            $_ -match "Out of host capacity" -or
            $_ -match "Further Information:" -or
            $_ -match "OPC request ID:" -or
            $_ -match "Apply complete"
        } |
        ForEach-Object { Write-Host $_ }
}

if (-not $CompartmentOcid) {
    $CompartmentOcid = Get-TenancyOcidFromOciConfig
}

if (-not $TerraformDirectory) {
    $TerraformDirectory = Join-Path $PSScriptRoot "..\..\infra\oci"
}

if (-not $SshPublicKeyPath) {
    $SshPublicKeyPath = Join-Path $env:USERPROFILE ".ssh\convy_oci_deploy.pub"
}

$resolvedTerraformDirectory = (Resolve-Path -LiteralPath $TerraformDirectory).Path
$TerraformDirectory = $resolvedTerraformDirectory
$normalizedSshPublicKeyPath = $SshPublicKeyPath -replace "\\", "/"
$planPath = Join-Path $resolvedTerraformDirectory "tfplan-heartbeat"

$placements = @(
    [pscustomobject]@{ Name = "default placement"; FaultDomain = $null },
    [pscustomobject]@{ Name = "FAULT-DOMAIN-1"; FaultDomain = "FAULT-DOMAIN-1" },
    [pscustomobject]@{ Name = "FAULT-DOMAIN-2"; FaultDomain = "FAULT-DOMAIN-2" },
    [pscustomobject]@{ Name = "FAULT-DOMAIN-3"; FaultDomain = "FAULT-DOMAIN-3" }
)

foreach ($placement in $placements) {
    Remove-Item -LiteralPath $planPath -Force -ErrorAction SilentlyContinue

    Write-Host "Retrying OCI A1 provisioning with $($placement.Name)..."

    $planArguments = @(
        "plan",
        "-no-color",
        "-var", "compartment_ocid=$CompartmentOcid",
        "-var", "ssh_public_key_path=$normalizedSshPublicKeyPath",
        "-var", "instance_ocpus=$InstanceOcpus",
        "-var", "instance_memory_gb=$InstanceMemoryGb",
        "-out=tfplan-heartbeat"
    )

    if ($placement.FaultDomain) {
        $planArguments += @("-var", "fault_domain=$($placement.FaultDomain)")
    }

    $plan = Invoke-Terraform -Arguments $planArguments
    if ($plan.ExitCode -ne 0) {
        Write-RelevantTerraformOutput -Output $plan.Output
        throw "terraform plan failed for $($placement.Name)."
    }

    $apply = Invoke-Terraform -Arguments @("apply", "-no-color", "-auto-approve", "tfplan-heartbeat")
    if ($apply.ExitCode -eq 0) {
        Write-RelevantTerraformOutput -Output $apply.Output
        Write-Host "OCI A1 provisioning succeeded with $($placement.Name)."
        exit 0
    }

    Write-RelevantTerraformOutput -Output $apply.Output

    if ($apply.Output -notmatch "Out of host capacity") {
        throw "terraform apply failed for $($placement.Name) without an OCI capacity error."
    }

    Write-Host "OCI still reports Out of host capacity for $($placement.Name)."
}

Write-Warning "All OCI A1 Always Free placement retries returned Out of host capacity."
exit 20
