# Explicit shared Traefik boundary

This companion prepares operational source only. Hetzner is shared staging. The installed immutable broker/controller, root profile and both activation gates remain unchanged; parsing stays `gpt-5.4-nano` with `modelPolicy: preserve`.

The shared infrastructure migration owns the public edge and its private content responder. Caddy is removed only through a reviewed application Compose override. A profile without `edgeTopology` retains every existing Caddy requirement. A profile with that field must have kind `traefik-file-v1`, an exact reviewed infrastructure SHA, approval digest and both live edge container IDs/images/configuration/mount/network fingerprints. The responder must share the proxy's namespace and preserve the exact existing legal/public readonly directory binds. Missing, stopped, replaced, reconfigured or differently owned edge containers fail before application backup/activation.

The frozen controller still verifies signed artifact/base-Compose/schema identities, complete ordered Compose source hashes, runtime environment/mount/network adoption, protected credentials, resource budgets, recovery and unaffected containers. It never recreates the independently managed edge. Static publishing retains root inodes and atomic file replacement; a new responder sees updates without restart. Generated certificate state belongs to the separately leased, validated certificate worker; route/static/auth/configuration bytes stay pinned, and unexpected changes block CD.

After separate migration/installation approval, append the infrastructure `convy-applications.override.yml` after all current Compose inputs. Validate with the installed Compose 5.1.3 that only Caddy is removed, every application/database service is preserved and external network identities remain exact. Then render a separate review artifact:

```sh
python3 ops/vps/prepare-edge-profile.py --profile CURRENT_DISABLED_PROFILE \
  --infrastructure-source-sha EXACT_REVIEWED_INFRASTRUCTURE_SHA \
  --applications-override ABSOLUTE_REVIEWED_OVERRIDE --output NEW_PRIVATE_REVIEW_PROFILE
```

This command does not install its output. Operator must review the exact old/new profile diff and freeze the updated controller plus `edge_topology.py` outside mutable `current`, with protected wrappers/ancestors. Never bypass Compose pins or reuse the old immutable broker with a relaxed profile. Keep automaticEnabled false and the GitHub variable disabled while installing/revalidating the topology. Model, credentials, schema, backup retention, artifact provenance and existing recovery objectives are preserved.

The generated profile includes the frozen `edge_topology.py` in its protected preservation list. Profiles adopting Traefik must retain this tool pin, and plan/apply approval digests include its exact bytes alongside the existing controller/content/shared-lock tool identities. This prepares a new reviewed trusted version; the installed frozen version is never edited by source preparation.

The identical shared guard in both repositories also refuses ordinary writers when the root-private interrupted-edge barrier exists. Only the separately approved edge recovery entry point can bypass that barrier while still acquiring and validating the same kernel lease. The common inode is never replaced. Complete pending writer installation and installed contention/nesting/interruption tests before a live migration; source fixtures do not prove live coordination.
