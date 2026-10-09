# Deployment Runbook

Hetzner is shared staging. Production does not exist. Use [automatic shared staging CD](staging-cd.md) for the normal developer flow, retention/backup policy, failure matrix and separately approved first activation. New source is inactive and the old workflow remains disabled.

After activation, reviewed compatible master pushes with successful exact CI deploy automatically. Exceptional manual recovery uses the fixed installed controller and checksummed journal in [isolated release and rollback](safe-release.md). Never rebuild an old source release or recreate the shared stack for rollback.

Initial Firebase/secret provisioning remains documented in [the VPS runbook](hetzner-vps-runbook.md). Its broad secret-push command is an initial provisioning operation, not the Luna pricing update mechanism. Preserve existing credentials during release. Backup installation/scheduling and OCI fallback remain separate operational procedures.
