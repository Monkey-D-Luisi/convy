<#
.SYNOPSIS
    Layer guard hook — blocks Domain layer from importing Infrastructure/EF Core.
    Used by VS Code Copilot PreToolUse hooks.
.NOTES
    Reads JSON from stdin. Returns JSON with permissionDecision.
    Exits quickly for non-file-editing tools to avoid noise.
#>
param()
$ErrorActionPreference = 'SilentlyContinue'

try {
    $raw = [Console]::In.ReadToEnd()
    $data = $raw | ConvertFrom-Json

    $tool = $data.toolName
    $editTools = @('replace_string_in_file', 'multi_replace_string_in_file', 'create_file')

    # Fast exit for non-editing tools
    if ($tool -notin $editTools) {
        return
    }

    $path = $data.toolInput.filePath
    if (-not $path) { return }

    # Only guard Domain layer (exclude test projects)
    if ($path -notmatch 'Convy\.Domain' -or $path -match 'Tests') {
        return
    }

    # Check content for forbidden imports
    $content = ''
    if ($data.toolInput.content)   { $content = $data.toolInput.content }
    if ($data.toolInput.newString)  { $content = $data.toolInput.newString }

    $forbidden = 'using Convy\.Infrastructure|using Microsoft\.EntityFrameworkCore|using Npgsql|using Convy\.Application'

    if ($content -match $forbidden) {
        @{
            hookSpecificOutput = @{
                hookEventName            = 'PreToolUse'
                permissionDecision       = 'deny'
                permissionDecisionReason = 'Domain layer must not reference Infrastructure, Application, or EF Core (Clean Architecture violation)'
            }
        } | ConvertTo-Json -Depth 5
    }
    # No output = allow (silent pass)
}
catch {
    # Swallow errors — never block the agent due to hook failures
}
