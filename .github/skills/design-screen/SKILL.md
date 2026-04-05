---
name: design-screen
description: "Workflow for designing a mobile screen using Stitch MCP. Use when creating UI mockups, screen layouts, or design variants for Convy screens."
---
# Design Screen Workflow

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
Structure the prompt for Stitch MCP:

```
Screen: {Screen Name}
App: Convy — Shared household coordination app
Platform: Mobile (Android, Material Design 3)
Theme: Light mode primary, generate dark mode variant too

PURPOSE: {One-line screen purpose}

LAYOUT (top to bottom):
- Top app bar: {title, actions}
- Body: {main content description}
- Bottom: {FAB, bottom bar, etc.}

COMPONENTS:
- {Component 1}: {description and behavior}
- {Component 2}: {description and behavior}

STATES:
- Default: {description}
- Empty: {what shows when no data}
- Loading: {skeleton or spinner}
- Error: {error message display}

INTERACTIONS:
- Tap {element}: {action}
- Swipe {element}: {action}
- Long press {element}: {action}

DESIGN TOKENS:
- Follow Material 3 color system
- Primary actions use primary color
- Completed items use muted/secondary styling
- Typography: Material 3 type scale
```

### Step 3: Generate via Stitch
1. Create or reuse a Stitch project via `mcp_stitch_create_project` (title: "Convy").
2. Generate the screen via `mcp_stitch_generate_screen_from_text` with:
   - `projectId`: the Stitch project ID
   - `prompt`: the structured prompt from Step 2
   - `deviceType`: `MOBILE`
   - `modelId`: `GEMINI_3_FLASH` (or `GEMINI_3_1_PRO` for higher quality)
3. Retrieve the result via `mcp_stitch_get_screen` if needed.

### Step 4: Review and Iterate
- Verify alignment with product principles (speed, one-hand use, clarity).
- Check all states are designed.
- Generate variants via `mcp_stitch_generate_variants` with:
  - `selectedScreenIds`: IDs of screens to vary
  - `variantOptions`: `{ "variantCount": 3, "creativeRange": "EXPLORE", "aspects": ["COLOR_SCHEME"] }` for dark mode
  - Use `"aspects": ["LAYOUT"]` to explore alternative layouts.

### Step 5: Document
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
