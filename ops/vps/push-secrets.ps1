param(
    [Parameter(Mandatory = $true)]
    [string] $HostName,

    [string] $SshUser = "root",
    [string] $SshKeyPath = "$env:USERPROFILE\.ssh\convy_vps_deploy",
    [string] $FirebaseAdminJsonPath = "$env:USERPROFILE\secrets\convy-firebase-admin.json",
    [string] $LocalEnvPath = "C:\Users\luiss\source\repos\convy\.env",
    [string] $ConvyHostname = "",
    [string] $PostgresPassword = ""
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SshKeyPath)) {
    throw "SSH key not found: $SshKeyPath"
}

if (-not (Test-Path $FirebaseAdminJsonPath)) {
    throw "Firebase Admin JSON not found: $FirebaseAdminJsonPath"
}

if ([string]::IsNullOrWhiteSpace($ConvyHostname)) {
    $ConvyHostname = "$HostName.nip.io"
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

$openAiApiKey = $env:OPENAI_API_KEY
if ([string]::IsNullOrWhiteSpace($openAiApiKey) -and (Test-Path $LocalEnvPath)) {
    $line = Get-Content $LocalEnvPath | Where-Object { $_ -match '^OPENAI_API_KEY=' } | Select-Object -First 1
    if ($line) {
        $openAiApiKey = $line.Substring("OPENAI_API_KEY=".Length)
    }
}

if ([string]::IsNullOrWhiteSpace($openAiApiKey)) {
    throw "OPENAI_API_KEY was not found in the process environment or $LocalEnvPath"
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("convy-vps-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $apiEnv = Join-Path $tempDir "api.env"
    @(
        "POSTGRES_DB=convy"
        "POSTGRES_USER=convy"
        "POSTGRES_PASSWORD=$PostgresPassword"
        "CONVY_HOSTNAME=$ConvyHostname"
        "FIREBASE_PROJECT_ID=convy-6520d"
        "OPENAI_API_KEY=$openAiApiKey"
        "DATABASE_MIGRATE_ON_STARTUP=true"
        "OpenAI__TranscriptionModel=gpt-4o-mini-transcribe"
        "OpenAI__ParsingModel=gpt-5.4-nano"
        "PushNotifications__BatchWindowSeconds=60"
    ) | Set-Content -Path $apiEnv -Encoding ascii

    scp -i $SshKeyPath $apiEnv "${SshUser}@${HostName}:/tmp/convy-api.env"
    scp -i $SshKeyPath $FirebaseAdminJsonPath "${SshUser}@${HostName}:/tmp/convy-firebase-admin.json"

    ssh -i $SshKeyPath "${SshUser}@${HostName}" "sed -i 's/\r$//' /tmp/convy-api.env && install -m 600 -o root -g root /tmp/convy-api.env /opt/convy/shared/api.env && install -m 644 -o root -g root /tmp/convy-firebase-admin.json /opt/convy/shared/firebase-admin.json && rm -f /tmp/convy-api.env /tmp/convy-firebase-admin.json"
}
finally {
    Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
}
