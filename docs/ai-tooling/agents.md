# AI Agent Guidance

Convy uses layered governance so AI assistants can work safely across backend, mobile, infrastructure, and documentation.

## Sources Of Truth

1. `AGENTS.md` for workspace-wide standards.
2. Subfolder `AGENTS.md` files for backend/mobile-specific rules.
3. `.github/instructions/*.instructions.md` for file-pattern rules.
4. `.github/skills/*/SKILL.md` plus `.claude/skills/*/SKILL.md` and `.agents/skills/*/SKILL.md` for workflow procedures.
5. `.github/hooks/` and `.claude/settings.json` for deterministic guardrails.

## Documentation Work Rules

- Document the code and infrastructure that exist in the target branch.
- Label future work as backlog or future work.
- Do not add real secrets, hashes, tokens, emails, private keys, or credentials.
- Keep ChatGPT MCP user integration docs under `docs/mcp/`.
- Keep development-agent MCP setup under `docs/ai-tooling/`.
- Update links when docs are moved or renamed.
- Preserve the Android `applicationId` as `com.monkeydluisi.convy`.

## Useful Prompts

```text
Using the code-review skill, review docs/ARCHITECTURE.md against backend/src and docker/.
```

```text
Using the test-suite skill, verify the backend and MCP tests affected by this change.
```

```text
Using the firebase-setup skill, verify auth-domain requirements before changing OAuth or dashboard docs.
```

## MCP Terminology

When writing docs, use explicit names:

- "ChatGPT MCP" or "Convy ChatGPT MCP" for the user-facing integration at `mcp.convyapp.com`.
- "AI development MCP tooling" for GitHub/PostgreSQL/Context7/Stitch/Maestro servers used by coding agents.
