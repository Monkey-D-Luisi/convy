# Deployment Runbook

The reviewed replacement VPS procedure is [isolated release and rollback](safe-release.md). The legacy automatic master deployment and manual source-build commands are unsafe on a shared host and must not be used.

Before any merge, separately authorize disabling the installed Backend Staging Release workflow and cancelling its pending runs. The branch workflow stub does not control master until that ordered transition is approved and completed. Build immutable artifacts only on trusted CI/workstations; production requires a reviewed protected profile, current recovery evidence, exact artifact/plan digests and explicit release approval.

Initial Firebase/secret provisioning remains documented in [the VPS runbook](hetzner-vps-runbook.md). Its broad secret-push command is an initial provisioning operation, not the Luna pricing update mechanism. Preserve existing credentials during release. Backup installation/scheduling and OCI fallback remain separate operational procedures.
