---
description: "Design a mobile screen using Stitch MCP. Generate UI mockups with proper Material 3 styling for Convy screens."
mode: "agent"
tools: ["read", "search", "stitch/*"]
---
# Design UI

Design a Convy mobile screen using the Stitch MCP.

## Input
- `$input` — Screen to design. Reference the MVP spec section if available (e.g., "Screen 4: List Detail" from Section 12.4).

## Process
1. **Read Skill**: Read `.github/skills/design-screen/SKILL.md` first — follow the procedure exactly.
2. **Requirements**: Read the screen spec from `docs/mvp-spec.md` (Sections 11-12).
3. **Context**: Check existing screens in `mobile/composeApp/src/commonMain/` for visual consistency.
4. **MVI State**: Check if the screen's state/intent is already defined in `mobile/shared/src/commonMain/` to understand available data.
5. **Design**: Compose the Stitch prompt using the template from the skill and generate via `mcp_stitch_generate_screen_from_text` in project `5694262812667273070` with `modelId: "GEMINI_3_1_PRO"`.
6. **Variants**: Generate the mandatory dark mode variant via `mcp_stitch_generate_variants` with `aspects: ["COLOR_SCHEME"]`; generate layout variants only when explicitly useful.
7. **Review**: Ask the user to review the light and dark designs before implementation.
8. **Fallback**: If Stitch MCP is unavailable, produce a detailed markdown design spec with: layout (top-to-bottom), components list, all states (default/empty/loading/error), interactions, and Material 3 design tokens.

## Output
- Screen design(s) generated in Stitch (or markdown spec as fallback)
- All states designed: default, empty, loading, error
- Dark mode variant
- Design rationale and key decisions documented
