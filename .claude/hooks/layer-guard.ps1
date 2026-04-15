<#
.SYNOPSIS
    Layer guard hook — blocks Domain layer from importing Infrastructure/EF Core.
    Used by Claude Code PreToolUse hooks.
.NOTES
    Reads JSON from stdin (snake_case format). Returns JSON with decision.
    Exits quickly for non-file-editing tools to avoid noise.
#>
param()
$ErrorActionPreference = 'SilentlyContinue'

try {
    $raw = [Console]::In.ReadToEnd()
    $data = $raw | ConvertFrom-Json

    $tool = $data.tool_name
    $editTools = @('write', 'edit', 'create')

    # Fast exit for non-editing tools
    if ($tool -notin $editTools) {
        exit 0
    }

    $path = $data.tool_input.file_path
    if (-not $path) { exit 0 }

    # Only guard Domain layer (exclude test projects)
    if ($path -notmatch 'Convy\.Domain' -or $path -match 'Tests') {
        exit 0
    }

    # Check content for forbidden imports
    $content = ''
    if ($data.tool_input.content) {
        $content = $data.tool_input.content
    }
    elseif ($data.tool_input.new_string) {
        $content = $data.tool_input.new_string
    }

    $forbidden = 'using Convy\.Infrastructure|using Microsoft\.EntityFrameworkCore|using Npgsql|using Convy\.Application'

    if ($content -match $forbidden) {
        Write-Output '{"decision": "block", "reason": "Domain layer must not reference Infrastructure, Application, or EF Core (Clean Architecture violation)"}'
    }
    # No output = allow (silent pass)
}
catch {
    # Swallow errors — never block the agent due to hook failures
}
