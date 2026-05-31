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

function Get-ExistingRemoteEnvValue {
    param([string] $Name)

    $command = "if [ -f /opt/convy/shared/api.env ]; then sed -n 's/^$Name=//p' /opt/convy/shared/api.env | head -n 1; fi"
    $output = & ssh -i $SshKeyPath "${SshUser}@${HostName}" $command 2>$null
    if ($LASTEXITCODE -ne 0) {
        return ""
    }

    $value = $output | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($value)) {
        return ""
    }

    return $value.Trim()
}

function Get-LocalEnvValue {
    param([string] $Name)

    $processValue = [Environment]::GetEnvironmentVariable($Name)
    if (-not [string]::IsNullOrWhiteSpace($processValue)) {
        return $processValue
    }

    if (Test-Path $LocalEnvPath) {
        $escapedName = [regex]::Escape($Name)
        $line = Get-Content $LocalEnvPath | Where-Object { $_ -match "^$escapedName=" } | Select-Object -First 1
        if ($line) {
            return $line.Substring($Name.Length + 1)
        }
    }

    return ""
}

function Get-SecretValue {
    param(
        [string] $LocalName,
        [string] $RemoteName
    )

    $value = Get-LocalEnvValue -Name $LocalName
    if (-not [string]::IsNullOrWhiteSpace($value)) {
        return $value
    }

    if ($LocalName -ne $RemoteName) {
        $value = Get-LocalEnvValue -Name $RemoteName
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value
        }
    }

    return Get-ExistingRemoteEnvValue -Name $RemoteName
}

function New-RandomSecret {
    $bytes = [byte[]]::new(32)
    $randomNumberGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $randomNumberGenerator.GetBytes($bytes)
    }
    finally {
        $randomNumberGenerator.Dispose()
    }

    return [Convert]::ToBase64String($bytes)
}

function New-McpKeyPair {
    try {
        $rsa = [System.Security.Cryptography.RSA]::Create(4096)
        try {
            $privatePem = $rsa.ExportPkcs8PrivateKeyPem()
            $publicPem = $rsa.ExportSubjectPublicKeyInfoPem()

            return @{
                PrivateKey = [Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($privatePem))
                PublicKey = [Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($publicPem))
            }
        }
        finally {
            $rsa.Dispose()
        }
    }
    catch {
        $remoteCommand = @'
tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:4096 -out "$tmp_dir/private.pem" >/dev/null 2>&1
openssl pkey -pubout -in "$tmp_dir/private.pem" -out "$tmp_dir/public.pem" >/dev/null 2>&1
base64 -w 0 "$tmp_dir/private.pem"
printf '\n'
base64 -w 0 "$tmp_dir/public.pem"
'@ -replace "`r?`n", "; "

        $output = & ssh -i $SshKeyPath "${SshUser}@${HostName}" $remoteCommand
        if ($LASTEXITCODE -ne 0 -or $output.Count -lt 2) {
            throw "Failed to generate MCP RSA key pair locally or on the remote host."
        }

        return @{
            PrivateKey = ($output | Select-Object -First 1).Trim()
            PublicKey = ($output | Select-Object -Skip 1 -First 1).Trim()
        }
    }
}

if (-not (Test-Path $SshKeyPath)) {
    throw "SSH key not found: $SshKeyPath"
}

if (-not (Test-Path $FirebaseAdminJsonPath)) {
    throw "Firebase Admin JSON not found: $FirebaseAdminJsonPath"
}

if ([string]::IsNullOrWhiteSpace($ConvyHostname)) {
    $ConvyHostname = "convyapp.com"
}

if ([string]::IsNullOrWhiteSpace($ConvyPublicHostname)) {
    $ConvyPublicHostname = $ConvyHostname
}

if ([string]::IsNullOrWhiteSpace($ConvyWwwHostname)) {
    $ConvyWwwHostname = "www.$ConvyHostname"
}

if ([string]::IsNullOrWhiteSpace($ConvyApiHostname)) {
    $ConvyApiHostname = "api.$ConvyHostname"
}

if ([string]::IsNullOrWhiteSpace($ConvyAdminHostname)) {
    $ConvyAdminHostname = "admin.$ConvyHostname"
}

if ([string]::IsNullOrWhiteSpace($ConvyAuthHostname)) {
    $ConvyAuthHostname = "auth.$ConvyHostname"
}

if ([string]::IsNullOrWhiteSpace($ConvyMcpHostname)) {
    $ConvyMcpHostname = "mcp.$ConvyHostname"
}

if ([string]::IsNullOrWhiteSpace($ConvyLegalHostname)) {
    $ConvyLegalHostname = "legal.$ConvyHostname"
}

if ([string]::IsNullOrWhiteSpace($ConvyLegacyApiHostname)) {
    $ConvyLegacyApiHostname = "$HostName.nip.io"
}

if ([string]::IsNullOrWhiteSpace($ConvyLegacyAdminHostname)) {
    $ConvyLegacyAdminHostname = "admin.$HostName.nip.io"
}

if ([string]::IsNullOrWhiteSpace($ConvyLegacyAuthHostname)) {
    $ConvyLegacyAuthHostname = "auth.$HostName.nip.io"
}

if ([string]::IsNullOrWhiteSpace($ConvyLegacyMcpHostname)) {
    $ConvyLegacyMcpHostname = "mcp.$HostName.nip.io"
}

if ([string]::IsNullOrWhiteSpace($ConvyLegacyLegalHostname)) {
    $ConvyLegacyLegalHostname = "legal.$HostName.nip.io"
}

if ([string]::IsNullOrWhiteSpace($PostgresPassword)) {
    $existingPostgresPasswordCommand = "if [ -f /opt/convy/shared/api.env ]; then sed -n 's/^POSTGRES_PASSWORD=//p' /opt/convy/shared/api.env | head -n 1; fi"
    $existingPostgresPasswordOutput = & ssh -i $SshKeyPath "${SshUser}@${HostName}" $existingPostgresPasswordCommand 2>$null
    if ($LASTEXITCODE -eq 0) {
        $existingPostgresPassword = $existingPostgresPasswordOutput | Select-Object -First 1
        if (-not [string]::IsNullOrWhiteSpace($existingPostgresPassword)) {
            $PostgresPassword = $existingPostgresPassword.Trim()
        }
    }

    if ([string]::IsNullOrWhiteSpace($PostgresPassword)) {
        $bytes = [byte[]]::new(32)
        $randomNumberGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        try {
            $randomNumberGenerator.GetBytes($bytes)
        }
        finally {
            $randomNumberGenerator.Dispose()
        }
        $PostgresPassword = [Convert]::ToBase64String($bytes)
    }
}

$openAiApiKey = Get-SecretValue -LocalName "OPENAI_API_KEY" -RemoteName "OPENAI_API_KEY"
if ([string]::IsNullOrWhiteSpace($openAiApiKey)) {
    throw "OPENAI_API_KEY was not found in the process environment, $LocalEnvPath, or the existing remote environment."
}

$adminBasicAuthUser = Get-SecretValue -LocalName "ADMIN_BASIC_AUTH_USER" -RemoteName "ADMIN_BASIC_AUTH_USER"
if ([string]::IsNullOrWhiteSpace($adminBasicAuthUser)) {
    $adminBasicAuthUser = "admin"
}
$adminBasicAuthHash = Get-SecretValue -LocalName "ADMIN_BASIC_AUTH_HASH" -RemoteName "ADMIN_BASIC_AUTH_HASH"
if ([string]::IsNullOrWhiteSpace($adminBasicAuthHash)) {
    throw "ADMIN_BASIC_AUTH_HASH was not found in the process environment, $LocalEnvPath, or the existing remote environment. Generate it with: docker run --rm caddy:2.10.0-alpine caddy hash-password --plaintext '<password>'"
}
$adminBasicAuthHashForCompose = $adminBasicAuthHash.Replace('$$', '$').Replace('$', '$$')

$adminAllowedEmails = Get-SecretValue -LocalName "ADMIN_ALLOWED_EMAILS" -RemoteName "Admin__AllowedEmails"
if ([string]::IsNullOrWhiteSpace($adminAllowedEmails)) {
    throw "ADMIN_ALLOWED_EMAILS was not found in the process environment, $LocalEnvPath, or the existing remote environment."
}

$firebaseWebApiKey = Get-SecretValue -LocalName "FIREBASE_WEB_API_KEY" -RemoteName "FIREBASE_WEB_API_KEY"
$firebaseAuthDomain = Get-SecretValue -LocalName "FIREBASE_AUTH_DOMAIN" -RemoteName "FIREBASE_AUTH_DOMAIN"
if ([string]::IsNullOrWhiteSpace($firebaseAuthDomain)) {
    $firebaseAuthDomain = "convy-6520d.firebaseapp.com"
}
$firebaseWebAppId = Get-SecretValue -LocalName "FIREBASE_WEB_APP_ID" -RemoteName "FIREBASE_WEB_APP_ID"
if ([string]::IsNullOrWhiteSpace($firebaseWebApiKey)) {
    throw "FIREBASE_WEB_API_KEY was not found in the process environment, $LocalEnvPath, or the existing remote environment."
}
if ([string]::IsNullOrWhiteSpace($firebaseWebAppId)) {
    throw "FIREBASE_WEB_APP_ID was not found in the process environment, $LocalEnvPath, or the existing remote environment."
}

$mcpPrivateKeyBase64 = Get-SecretValue -LocalName "MCP_AUTH_PRIVATE_KEY_PEM_BASE64" -RemoteName "McpAuth__PrivateKeyPemBase64"
$mcpPublicKeyBase64 = Get-SecretValue -LocalName "MCP_AUTH_PUBLIC_KEY_PEM_BASE64" -RemoteName "McpAuth__PublicKeyPemBase64"
if ([string]::IsNullOrWhiteSpace($mcpPrivateKeyBase64) -and [string]::IsNullOrWhiteSpace($mcpPublicKeyBase64)) {
    $mcpKeyPair = New-McpKeyPair
    $mcpPrivateKeyBase64 = $mcpKeyPair.PrivateKey
    $mcpPublicKeyBase64 = $mcpKeyPair.PublicKey
}
elseif ([string]::IsNullOrWhiteSpace($mcpPrivateKeyBase64) -or [string]::IsNullOrWhiteSpace($mcpPublicKeyBase64)) {
    throw "Both MCP_AUTH_PRIVATE_KEY_PEM_BASE64 and MCP_AUTH_PUBLIC_KEY_PEM_BASE64 must be provided together."
}

$mcpAuditApiKey = Get-SecretValue -LocalName "CONVY_MCP_AUDIT_API_KEY" -RemoteName "McpAudit__ApiKey"
if ([string]::IsNullOrWhiteSpace($mcpAuditApiKey)) {
    $mcpAuditApiKey = New-RandomSecret
}

$openAiAppsChallengeToken = Get-SecretValue -LocalName "OPENAI_APPS_CHALLENGE_TOKEN" -RemoteName "OPENAI_APPS_CHALLENGE_TOKEN"

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("convy-vps-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $apiEnv = Join-Path $tempDir "api.env"
    @(
        "POSTGRES_DB=convy"
        "POSTGRES_USER=convy"
        "POSTGRES_PASSWORD=$PostgresPassword"
        "CONVY_PUBLIC_HOSTNAME=$ConvyPublicHostname"
        "CONVY_WWW_HOSTNAME=$ConvyWwwHostname"
        "CONVY_API_HOSTNAME=$ConvyApiHostname"
        "CONVY_ADMIN_HOSTNAME=$ConvyAdminHostname"
        "CONVY_AUTH_HOSTNAME=$ConvyAuthHostname"
        "CONVY_MCP_HOSTNAME=$ConvyMcpHostname"
        "CONVY_LEGAL_HOSTNAME=$ConvyLegalHostname"
        "CONVY_LEGACY_API_HOSTNAME=$ConvyLegacyApiHostname"
        "CONVY_LEGACY_ADMIN_HOSTNAME=$ConvyLegacyAdminHostname"
        "CONVY_LEGACY_AUTH_HOSTNAME=$ConvyLegacyAuthHostname"
        "CONVY_LEGACY_MCP_HOSTNAME=$ConvyLegacyMcpHostname"
        "CONVY_LEGACY_LEGAL_HOSTNAME=$ConvyLegacyLegalHostname"
        "CONVY_STAGING_IP=$HostName"
        "Convy__PublicHostname=$ConvyPublicHostname"
        "Convy__LegalHostname=$ConvyLegalHostname"
        "Convy__StagingIp=$HostName"
        "FIREBASE_PROJECT_ID=convy-6520d"
        "FIREBASE_WEB_API_KEY=$firebaseWebApiKey"
        "FIREBASE_AUTH_DOMAIN=$firebaseAuthDomain"
        "FIREBASE_WEB_APP_ID=$firebaseWebAppId"
        "OPENAI_API_KEY=$openAiApiKey"
        "OPENAI_APPS_CHALLENGE_TOKEN=$openAiAppsChallengeToken"
        "VOICE_PARSING_ENABLED=true"
        "DATABASE_MIGRATE_ON_STARTUP=true"
        "ADMIN_BASIC_AUTH_USER=$adminBasicAuthUser"
        "ADMIN_BASIC_AUTH_HASH=$adminBasicAuthHashForCompose"
        "Admin__AllowedEmails=$adminAllowedEmails"
        "McpAuth__Issuer=https://$ConvyAuthHostname"
        "McpAuth__Audience=https://$ConvyMcpHostname"
        "McpAuth__AuthorizationEndpoint=https://$ConvyAuthHostname/oauth/authorize"
        "McpAuth__PrivateKeyPemBase64=$mcpPrivateKeyBase64"
        "McpAuth__PublicKeyPemBase64=$mcpPublicKeyBase64"
        "McpAuth__AllowedClientMetadataHosts__0=chat.openai.com"
        "McpAuth__AllowedClientMetadataHosts__1=chatgpt.com"
        "McpAudit__ApiKey=$mcpAuditApiKey"
        "OpenAI__TranscriptionModel=gpt-4o-mini-transcribe"
        "OpenAI__ParsingModel=gpt-5.4-nano"
        "OpenAI__Costs__TranscriptionAudioInputMicrosPerSecond=$env:OPENAI_COST_TRANSCRIPTION_AUDIO_MICROS_PER_SECOND"
        "OpenAI__Costs__ParsingInputMicrosPer1KTokens=$env:OPENAI_COST_PARSING_INPUT_MICROS_PER_1K_TOKENS"
        "OpenAI__Costs__ParsingCachedInputMicrosPer1KTokens=$env:OPENAI_COST_PARSING_CACHED_INPUT_MICROS_PER_1K_TOKENS"
        "OpenAI__Costs__ParsingOutputMicrosPer1KTokens=$env:OPENAI_COST_PARSING_OUTPUT_MICROS_PER_1K_TOKENS"
        "OpenAI__Costs__ParsingReasoningMicrosPer1KTokens=$env:OPENAI_COST_PARSING_REASONING_MICROS_PER_1K_TOKENS"
        "Operations__BackupRoot=/opt/convy/backups/postgres"
        "PushNotifications__BatchWindowSeconds=60"
    ) | Set-Content -Path $apiEnv -Encoding ascii

    scp -i $SshKeyPath $apiEnv "${SshUser}@${HostName}:/tmp/convy-api.env"
    scp -i $SshKeyPath $FirebaseAdminJsonPath "${SshUser}@${HostName}:/tmp/convy-firebase-admin.json"

    ssh -i $SshKeyPath "${SshUser}@${HostName}" "sed -i 's/\r$//' /tmp/convy-api.env && install -m 600 -o root -g root /tmp/convy-api.env /opt/convy/shared/api.env && install -m 640 -o root -g 1654 /tmp/convy-firebase-admin.json /opt/convy/shared/firebase-admin.json && chmod 640 /opt/convy/shared/firebase-admin.json && rm -f /tmp/convy-api.env /tmp/convy-firebase-admin.json"
}
finally {
    Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
}
