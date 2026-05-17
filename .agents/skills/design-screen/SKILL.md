---
name: design-screen
description: "Workflow for designing a mobile screen using Stitch MCP. Use when creating UI mockups, screen layouts, or design variants for Convy screens."
---
# Design Screen Workflow

## MANDATORY RULES — DO NOT SKIP

1. **Model**: Always use `GEMINI_3_1_PRO` (`modelId: "GEMINI_3_1_PRO"`). Never use `GEMINI_3_FLASH` or default.
2. **Detailed prompt**: Every Stitch prompt MUST include ALL sections from the template below. Vague or incomplete prompts are not acceptable.
3. **Project**: Use the existing Stitch project **"Convy — Household Coordination App"** (ID: `5694262812667273070`). Do not create a new project.
4. **Dark mode**: Always generate a dark mode variant after the light mode screen using `mcp_stitch_generate_variants` with `aspects: ["COLOR_SCHEME"]`.
5. **User review**: After generating, ask the user via `vscode_askQuestions` if the design looks good before proceeding to implementation.

## When to Use
- Designing a new screen before implementation
- Creating design variants or exploring layout options
- Generating visual specs for the mobile developer

## Prerequisites
- Read `docs/mvp-spec.md` Sections 11-12 for navigation and screen specs.
- Check if the screen's MVI state is already defined (helps understand available data).

## Procedure

### Step 1: Gather Screen Requirements
From the spec, identify:
- Screen name and purpose
- Data to display
- User actions available
- Navigation context (where from, where to)
- All possible states (empty, loading, data, error)

### Step 2: Compose Stitch Prompt

**IMPORTANT**: The prompt MUST be extremely detailed and structured. Every section below is mandatory — do not omit or abbreviate any section. A well-defined prompt produces a high-quality design; a vague prompt produces unusable output.

Structure the prompt for Stitch MCP using this template (ALL sections required):

```
Screen: {Screen Name}
App: Convy — Shared household coordination app
Platform: Mobile (Android, Compose Multiplatform, Material Design 3)
Theme: Light mode primary, generate dark mode variant too
Design System: "Convy Hearth" — Primary #10B981 teal, Plus Jakarta Sans headlines, Be Vietnam Pro body, rounded 8dp shapes

PURPOSE: {One-line screen purpose describing the user goal}

USER CONTEXT: {Who sees this screen, when, and what they expect to do}

LAYOUT (top to bottom — be specific with exact spacing and sizing):
- Status bar: {transparent/themed}
- Top app bar: {title text, leading icon, trailing actions — specify each icon by name}
- Body: {main content — describe every visual element, their arrangement, padding, and visual weight}
- Bottom: {FAB position, bottom bar items, safe area}

COMPONENTS (describe EVERY UI element):
- {Component 1}: {exact appearance: background color, corner radius, padding, icon name, text style, elevation}
- {Component 2}: {exact appearance}
- ... {list ALL components visible on screen}

TYPOGRAPHY:
- Headlines: {font, weight, size — e.g. "Plus Jakarta Sans Bold 22sp"}
- Body: {font, weight, size}
- Labels/Captions: {font, weight, size}

COLOR USAGE:
- Background: {surface/surfaceContainerLow + hex}
- Cards: {surfaceContainerLowest + hex}
- Primary actions: {primary color + hex}
- Secondary text: {onSurfaceVariant + hex}
- Dividers/outlines: {outlineVariant + hex}

STATES (describe ALL possible states):
- Default/Loaded: {exact layout with sample data — include realistic example content}
- Empty: {centered icon + title + subtitle + CTA button}
- Loading: {skeleton shimmer pattern or circular indicator}
- Error: {error icon + message + retry button}

INTERACTIONS:
- Tap {element}: {exactly what happens — navigation target, dialog, animation}
- Swipe {element}: {action and visual feedback}
- Long press {element}: {action if applicable}
- Scroll behavior: {app bar collapse? sticky headers?}

SAMPLE DATA (provide realistic example content for the design):
- {Item 1}: {realistic text/values}
- {Item 2}: {realistic text/values}
- {Item 3}: {realistic text/values}

ACCESSIBILITY:
- Minimum touch target: 48dp
- Content descriptions for icons
- Sufficient color contrast (WCAG AA)
```

### Step 3: Generate via Stitch
1. Use the existing Stitch project ID: `5694262812667273070` (title: "Convy — Household Coordination App").
2. Generate the screen via `mcp_stitch_generate_screen_from_text` with:
   - `projectId`: `5694262812667273070`
   - `prompt`: the structured prompt from Step 2 (MUST include all sections)
   - `deviceType`: `MOBILE`
   - `modelId`: `GEMINI_3_1_PRO` (**MANDATORY** — never use FLASH or default)
3. Retrieve the result via `mcp_stitch_get_screen` if needed.
4. **Ask the user** via `vscode_askQuestions` if the design meets their expectations before proceeding.

### Step 4: Generate Dark Mode Variant (MANDATORY)
After the light mode screen is accepted:
1. Generate a dark mode variant via `mcp_stitch_generate_variants` with:
   - `selectedScreenIds`: [the screen ID from Step 3]
   - `variantOptions`: `{ "variantCount": 1, "creativeRange": "EXPLORE", "aspects": ["COLOR_SCHEME"] }`
2. Confirm with user via `vscode_askQuestions` that dark mode looks correct.

### Step 5: Review and Iterate
- Verify alignment with product principles (speed, one-hand use, clarity).
- Check all states are designed.
- For additional layout alternatives, use `mcp_stitch_generate_variants` with `"aspects": ["LAYOUT"]`.

### Step 6: Document
Save the design reference and key decisions for the mobile developer.

## Screen Template Library

### Convy Screen Patterns
| Pattern | Use For |
|---------|---------|
| List screen | Shopping list detail, task list detail |
| Form screen | Create/edit item |
| Card grid | Home/lists overview |
| Simple settings | Settings, profile |
| Onboarding flow | Auth, household setup |
