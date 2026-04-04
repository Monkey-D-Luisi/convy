# Convy — MCP Server Setup

## Overview

MCP (Model Context Protocol) servers extend AI agent capabilities. This project uses 4 MCP servers.

## Prerequisites

- Node.js 18+ (for `npx`)
- Docker running (for PostgreSQL MCP)
- GitHub personal access token (for GitHub MCP)

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

**Verify**: The AI can list issues, create PRs, and search code.

### 2. PostgreSQL MCP Server

**Purpose**: Query the database, inspect schema, and run SQL.

**Setup**:
1. Start the database: `docker compose -f docker/docker-compose.yml up db -d`
2. The connection string is preconfigured for local dev.

**Verify**: The AI can run `SELECT version()` and list tables.

### 3. Context7 MCP Server

**Purpose**: Look up library documentation and API references for any package.

**Setup**: No configuration needed. Works out of the box with `npx`.

**Verify**: Ask the AI to look up docs for a library (e.g., "MediatR pipeline behaviors").

### 4. Stitch MCP Server (Copilot only)

**Purpose**: Generate UI mockups and design screens.

**Setup**: Configure in VS Code settings or use the `@ui-designer` agent which has Stitch tools enabled.

**Note**: Stitch is available as a VS Code extension/MCP. Check the [Stitch docs](https://stitch.withgoogle.com/) for latest setup.

## Configuration Files

| Editor | File | Section |
|--------|------|---------|
| VS Code (Copilot) | `.vscode/settings.json` | `mcp.servers` |
| Claude Code | `.claude/settings.json` | `mcpServers` |

## Troubleshooting

- **"npx not found"**: Ensure Node.js is installed and `npx` is on PATH
- **PostgreSQL connection refused**: Ensure Docker is running and `docker compose up db` has started
- **GitHub 401**: Check that `GITHUB_TOKEN` is set and has the right scopes (repo, read:org)
