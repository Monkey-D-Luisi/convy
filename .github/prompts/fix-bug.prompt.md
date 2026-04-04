---
description: "Investigate and fix a bug. Analyze symptoms, trace root cause, implement fix, and add regression test."
agent: "agent"
---
# Fix Bug

Investigate and fix a bug in the Convy codebase.

## Input
Describe the bug: expected behavior, actual behavior, and steps to reproduce if known.

## Process
1. **Reproduce**: Understand the symptoms and identify affected area (backend/mobile).
2. **Trace**: Search codebase for the relevant code path. Read tests for expected behavior.
3. **Root cause**: Identify why the bug occurs.
4. **Fix**: Implement the minimal fix that resolves the issue.
5. **Regression test**: Add a test that would have caught this bug.
6. **Verify**: Run full test suite to ensure no regressions.

## Output
- Bug fix with clear explanation of root cause
- Regression test
- All existing tests still pass
