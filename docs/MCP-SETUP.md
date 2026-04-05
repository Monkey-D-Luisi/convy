# Convy — MCP Server Setup

## Overview

MCP (Model Context Protocol) servers extend AI agent capabilities. This project uses 4 MCP servers.

## Prerequisites

- Node.js 18+ (for `npx`)
- Docker running (for PostgreSQL MCP)
- GitHub personal access token (for GitHub MCP)
- VS Code with Stitch extension (for Stitch MCP)

## Server Setup

### 1. GitHub MCP Server

**Purpose**: Manage PRs, issues, code search, and repository operations.

**Setup**:
1. Create a GitHub Personal Access Token at https://github.com/settings/tokens
2. Set the `GITHUB_TOKEN` environment variable:
   ```powershell
   # Windows (User environment variable)
   [Environment]::SetEnvironmentVariable("GITHUB_TOKEN", "ghp_your_token_here", "User")
   ```

**Verify**: Ask the AI to list repository issues or create a branch.

**Tools available**: `mcp_io_github_git_*` — issue management, PR creation, code search, branch management.

### 2. PostgreSQL MCP Server

**Purpose**: Query the database, inspect schema, and run SQL.

**Setup**:
1. Start the database: `docker compose -f docker/docker-compose.yml up db -d`
2. The connection string is preconfigured for local dev.

**Verify**: Ask the AI to run `SELECT version()` or list tables with `\dt`.

**Tools available**: Direct SQL query execution against the Convy database.

### 3. Context7 MCP Server

**Purpose**: Look up library documentation and API references for any package.

**Setup**: No configuration needed. Works out of the box with `npx`.

**Verify**: Ask the AI to look up docs for a library (e.g., "MediatR pipeline behaviors", "FluentValidation rules", "Ktor auth plugin").

**Tools available**: Documentation lookup for any npm/NuGet/Maven package.

### 4. Stitch MCP Server

**Purpose**: Generate UI mockups, screen designs, and design variants for mobile screens.

**Setup**:
1. Install the **Stitch** extension in VS Code (provided by Google).
2. The MCP tools are automatically available once the extension is installed — no additional configuration needed in `.vscode/settings.json`.

**Verify**: Ask the AI to list Stitch projects (`mcp_stitch_list_projects`). If it returns results (or an empty list), Stitch is working.

**Tools available**:
| Tool | Purpose |
|------|---------|
| `mcp_stitch_create_project` | Create a new design project |
| `mcp_stitch_generate_screen_from_text` | Generate a screen design from a text prompt |
| `mcp_stitch_generate_variants` | Generate design variants (dark mode, layout alternatives) |
| `mcp_stitch_edit_screens` | Edit existing screens with a prompt |
| `mcp_stitch_list_projects` | List all Stitch projects |
| `mcp_stitch_list_screens` | List screens in a project |
| `mcp_stitch_get_screen` | Get details of a specific screen |
| `mcp_stitch_get_project` | Get details of a specific project |

**Usage in workflows**: The `/design-ui` prompt and `.github/skills/design-screen/SKILL.md` skill use Stitch to generate designs before mobile implementation.

## Configuration Files

| Editor | File | Section |
|--------|------|---------|
| VS Code (Copilot) | `.vscode/settings.json` | `mcp.servers` |
| Claude Code | `.claude/settings.json` | `mcpServers` |

Note: Stitch MCP is provided by the VS Code extension and does not need an entry in `settings.json`.

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `npx not found` | Node.js not installed | Install Node.js 18+ and ensure `npx` is on PATH |
| PostgreSQL connection refused | Docker not running | Run `docker compose -f docker/docker-compose.yml up db -d` |
| GitHub 401 Unauthorized | Token not set or expired | Check `GITHUB_TOKEN` env var has correct value and scopes (`repo`, `read:org`) |
| `mcp_stitch_*` tools not found | Stitch extension not installed | Install the Stitch extension from VS Code marketplace |
| Context7 timeout | Network issue | Retry — Context7 runs via `npx` and needs internet access |
