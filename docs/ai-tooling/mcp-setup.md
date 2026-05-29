# AI Development MCP Setup

This document is for MCP servers used by Copilot, Claude Code, Codex, and other development agents while working on Convy.

It is not the Convy ChatGPT MCP integration. The user-facing ChatGPT MCP integration is documented under [docs/mcp](../mcp/README.md).

## Servers

| Server | Purpose | Notes |
| --- | --- | --- |
| GitHub MCP | Repository, issue, PR, and code search workflows. | Requires a GitHub token with appropriate repository access. |
| PostgreSQL MCP | Local schema inspection and SQL queries. | Use only against local/dev databases unless explicitly approved. |
| Context7 MCP | Library and API documentation lookup. | Requires network access. |
| Stitch MCP | Mobile UI design mockups and variants. | Provided by the Stitch VS Code extension. |
| Maestro MCP | Android E2E automation and UI inspection. | Requires Maestro CLI and an emulator/device. |

## GitHub MCP

Purpose: manage PRs, issues, code search, and repository operations.

Setup:

```powershell
[Environment]::SetEnvironmentVariable("GITHUB_TOKEN", "<github-token>", "User")
```

Use a real token only in your local environment. Do not commit it or paste it into repository docs.

## PostgreSQL MCP

Purpose: inspect local development database schema and run local queries.

```bash
cd docker
docker compose up -d db
```

Only query production/staging databases when the task explicitly requires it and the operator has approved the access path.

## Context7 MCP

Purpose: look up package and framework documentation for .NET, Kotlin, Node, Next.js, and related libraries.

No repository configuration is required beyond the MCP client setup.

## Stitch MCP

Purpose: generate mobile UI mockups and design variants.

Requirements:

- VS Code with the Stitch extension installed.
- Existing Convy project references when using the project design workflow.

Use this for design exploration before implementing new mobile screens. Do not treat generated designs as product truth until reviewed.

## Maestro MCP

Purpose: run or debug Android E2E flows, take screenshots, inspect hierarchy, and tap/input on an emulator.

Requirements:

- Maestro CLI 2.4.0+
- Android emulator or device visible through `adb devices`
- Local app installed
- Backend and PostgreSQL running for flows that require API access

## Configuration Files

| Tooling surface | Configuration |
| --- | --- |
| VS Code / Copilot | `.vscode/settings.json` |
| Claude Code | `.claude/settings.json` |
| Codex | app-level connector/plugin configuration plus `AGENTS.md` |

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `npx not found` | Node.js missing from PATH. | Install Node.js 22 and restart the shell/editor. |
| PostgreSQL connection refused | Docker database is not running. | Run `cd docker && docker compose up -d db`. |
| GitHub 401 | Missing or expired token. | Regenerate token and update `GITHUB_TOKEN`. |
| Stitch tools missing | VS Code extension not installed or not active. | Install/enable Stitch and reload VS Code. |
| Maestro tools cannot find device | Emulator not running or ADB unavailable. | Start emulator and confirm `adb devices`. |
