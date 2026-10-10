param(
    [Parameter(Mandatory = $true)]
    [string] $HostName,

    [string] $SshUser = "root",
    [string] $SshKeyPath = "$env:USERPROFILE\.ssh\convy_vps_deploy",
    [string] $FirebaseAdminJsonPath = "$env:USERPROFILE\secrets\convy-firebase-admin.json",
    [string] $LocalEnvPath = "C:\Users\luiss\source\repos\convy\.env",
    [string] $ConvyHostname = "",
    [string] $ConvyPublicHostname = "",
    [string] $ConvyWwwHostname = "",
    [string] $ConvyApiHostname = "",
    [string] $ConvyAdminHostname = "",
    [string] $ConvyAuthHostname = "",
    [string] $ConvyMcpHostname = "",
    [string] $ConvyLegalHostname = "",
    [string] $ConvyLegacyApiHostname = "",
    [string] $ConvyLegacyAdminHostname = "",
    [string] $ConvyLegacyAuthHostname = "",
    [string] $ConvyLegacyMcpHostname = "",
    [string] $ConvyLegacyLegalHostname = "",
    [string] $PostgresPassword = ""
)

$ErrorActionPreference = "Stop"
throw "Legacy shared-staging secret push is retired. Use a separately reviewed administrator transaction under the common host lease; preserve existing model and credential bytes."
