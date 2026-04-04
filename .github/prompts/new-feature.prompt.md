---
description: "Orchestrate the creation of a full feature across backend and mobile. Coordinates domain modeling, API endpoint, and mobile screen implementation."
agent: "agent"
---
# New Feature

Implement a complete feature for the Convy app, spanning backend and mobile.

## Input
Describe the feature you want to implement. Reference the user story or MVP spec section if available.

## Process
1. **Analyze**: Read `docs/mvp-spec.md` for the requirement details.
2. **Backend**: Use the `/backend-feature` skill to implement Domain → Application → Infrastructure → API.
3. **Mobile**: Use the `/mobile-screen` skill if a new screen is needed.
4. **Tests**: Ensure tests are written alongside implementation.
5. **Review**: Run a quick `/code-review` to verify SOLID and architecture compliance.

## Output
- Working backend endpoint(s) with tests
- Mobile screen (if applicable) with previews
- All code compiles and tests pass
