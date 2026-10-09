# Release automation transition

Hetzner is shared staging; production does not exist. PR #33 completed the control transition. `Backend Staging Release` is disabled and its source only prints the boundary. Keep workflow ID `290743932` disabled.

Manual release approval was transitional. [Automatic shared staging CD](staging-cd.md) describes the replacement and its separately approved first activation. Ordinary compatible master releases after activation require no individual approval.

This integration preserves reviewed Luna source and isolated rollback, reconciled with the merged control master. Source review does not merge PR #32/#34, install tools, enable timers or change secrets/protections. The new workflow remains inactive.

First activation and unsafe migration/shared topology/credential changes require operator review. GitHub Pages publication remains expected and independent; Android publishing safeguards remain intact.
