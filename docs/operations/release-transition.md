# Release automation transition

This change removes automatic deployment from `Backend Staging Release`. A manual invocation only prints the release boundary. It cannot connect to a server. This branch does not control the workflow still installed on master. It also carries the previously reviewed OpenAPI/Testcontainers dependency and Android SDK installation corrections needed to validate the master-based control PR. It contains no Luna parsing or release-controller changes.

Before **any merge**, obtain explicit owner approval to disable the installed workflow through the Actions API/UI, cancel queued/in-progress runs of that workflow, and verify it remains disabled with no deployment runs. Disabling a workflow is a GitHub control-plane change requiring separate approval. It must happen before merging this PR, the Luna PR, or the release implementation PR.

Merge this workflow-only control PR first after approval and green CI. Read the installed master workflow and confirm the `workflow_run` trigger, SSH, staging environment, and deployment steps are absent. Leave the old workflow disabled. Only then review/approve subsequent source merges. A green CI run is never authorization to release.

Build immutable images in trusted CI or a workstation from the exact reviewed commit. A future production release additionally requires review of its manifest digest, successful exact-head CI, current protected host profile and dry-run digest, backup/recovery proof, capacity, isolated rollback tests, and explicit owner approval. Do not enable another production workflow or alter environment protections as part of this control PR.
