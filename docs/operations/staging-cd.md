# Automatic deployment to shared staging

Hetzner is **shared staging**. There is no production environment. Local development uses local Docker and developer credentials; a future production environment will require its own topology, recovery objectives and credentials.

The normal target flow is `PR -> review/CI -> merge master -> automatic staging release -> acceptance -> automatic cleanup`. This source is prepared for review and remains inactive: the repository activation variable is absent/false, the broker has not been installed, and no timers or environment protections have been changed. First activation requires a separate operator decision. Ordinary compatible releases after activation do not ask for individual approval.

## Historical decisions and retained purposes

| Existing behavior | Original purpose | Current risk | Decision | Evidence |
| --- | --- | --- | --- | --- |
| Build source on the VPS | Simple low-cost single-host delivery without a registry/build fleet | Competes with databases and both products for two CPUs, memory and disk; rebuilt rollback bytes differ | Build immutable artifacts on GitHub-hosted runners | Original VPS hosting/deploy history, including `c7260c4376034a4c676386b1e25fbc31d3d6cc53` and May 2026 VPS runbooks |
| `docker image prune -f --filter until=168h` | Recover unused image storage; owner confirms disk exhaustion actually occurred | Dangling previous images can be needed by rollback despite having no container reference | Replace with a ledger of release-owned image IDs, checking all products' containers and protected journals | Historical `deploy-release.sh`; default prune affects dangling images, not all images |
| Buildx cache cap of 4 GB | Bound accumulation after healthy deployment | Builder remains on the shared host; legacy cache ownership is uncertain | Preserve 4 GB on each dedicated CI builder, prune that builder after each image, remove that builder at job exit | `62918d8e2ee7a4db3cb7cca25f28eb5c22f2b976`, “cap vps docker build cache,” May 30 2026 |
| Release directories/prior images | Permit rollback | A source directory alone does not prove that prior image bytes can be restored | Checksummed current and rollback archives plus independent configuration/source journals | Reviewed isolated release controller and Docker rollback tests |
| Daily/weekly/monthly dump retention | Database recovery with bounded storage | Timers and verified off-host recovery have not been activated | Preserve existing 7/35/120-day buckets; add fresh release-specific isolated restore and encrypted retrieval | `ops/vps/backups/*`; restore drills were previously separate/manual |
| Compose bind-directory preservation | Prevent Docker creating the wrong bind path during deployment | A new directory or changed edge override can alter unrelated services | Adopt all effective Compose files, existing mounts/networks/volumes and byte-preserved configuration | May 29 bind-mount fixes; shared-edge integration |
| Shared edge overrides | Host Convy and Converso behind one Caddy, preserving separate data stores | Single-file Compose deployment can remove routes or recreate Caddy | Keep ordered overrides/environment files and explicit application service selection | Existing shared-host topology and read-only effective Compose capture |
| Disable old automatic release | Stop unsafe host-wide rebuild/recreation while replacing the process | Treating this transitional measure as permanent defeats intended staging CD | Keep old workflow disabled; activate the new constrained workflow once reviewed | Merged control PR #33; owner staging clarification |

The owner-reported disk incident is operational evidence. The available Git history proves the cache cap's purpose and timing; it does not establish an exact incident date or a complete forensic disk log. Do not invent either.

## Compose versus Swarm

The shared CX23 has two CPUs, approximately 4 GiB RAM and 40 GB disk. Both products, edge, PostgreSQL, MariaDB and host operations compete for these resources. Actual headroom and service inventory are recorded in the private iteration report; snapshots do not promise spare capacity.

| Dimension | Current Compose | Single-node Swarm |
| --- | --- | --- |
| Resource cost | Existing Engine/Compose; serialized application recreation | Manager/Raft/control-loop work; rolling overlap still needs spare application memory |
| Isolation | Explicit `up --no-deps --no-build --pull never service`; shared containers fingerprinted | Services can be updated independently, but all existing Compose/container ownership must first migrate |
| Persistence | Existing bind mounts, volumes and networks adopted unchanged | Local bind/volume data stays node-local; Swarm does not add database HA or replicate it |
| Rollback | Restores exact prior images/configuration with a verified journal | Service spec rollback exists; database schema, secrets, bind files and shared edge still need separate recovery |
| Concurrency | One host lock spans both repositories and resource maintenance | Swarm scheduling does not serialize cross-product builds/backups/schema operations |
| Migration/complexity | No orchestrator migration | Network/service/secret/health/persistence conversion and a new failure model; one manager remains a single point of failure |

**Retain Compose.** There is no measured material benefit justifying a migration on this one small shared host. No Swarm installation is part of this change. See [Docker's Compose guidance](https://docs.docker.com/compose/how-tos/production/), [Swarm services](https://docs.docker.com/engine/swarm/services/) and [manager/node behavior](https://docs.docker.com/engine/swarm/how-swarm-mode-works/nodes/).

## Pipeline and trust boundaries

1. `staging-cd.yml` listens only to completed `Continuous Integration` runs. Its disabled-by-default flag precedes every privileged job. It permits the authorized repository, successful push CI on master, same head repository and exact workflow/source SHA.
2. `staging_common.py` re-fetches the authorized CI workflow ID, full SHA, current master, latest CI run/attempt and CD workflow identity. Superseded master runs fail closed; a duplicate accepted commit is a no-op. CI run IDs cannot move the accepted ledger backwards.
3. `staging-build.py` archives the exact Git tree, builds Linux/amd64 outside staging with a dedicated disposable builder and checks frontend runtime dependency closure. All application images are built so a previously skipped master cannot leave a changed application behind. Source context hashes let the host recreate only affected services.
4. The manifest binds source/baseline, CI/CD run identity, migration catalog, base Compose, image IDs/expanded sizes/context hashes, and both archive checksums. GitHub signs this manifest. Verification requires the exact repository, signer workflow, master certificate identity, source/signer digest, ref and a hosted runner. A PR or fork attestation cannot meet that policy. See [GitHub workflow-run trust boundaries](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_run) and [CLI attestation verification](https://cli.github.com/manual/gh_attestation_verify).
5. The deployment job uses the existing `staging` environment boundary, read permissions and a dedicated constrained `convy-cd` SSH principal. It pins the established ed25519 server key and known-hosts file digest. It never uses a root SSH key or bootstraps a user during release.
6. The forced command invokes a fixed installed root broker. Its bounded stdin protocol accepts five plain files and rejects links, duplicate names, unexpected files, oversized metadata, partial transfer and checksum changes. Uploaded scripts are never run.
7. The broker takes `/run/lock/shared-staging-deployment.lock` before resource-consuming transfer, recovery, loading, backup, application changes or cleanup. A competing product receives a busy result and retries later. GitHub concurrency is an additional per-repository queue, not the shared lock.
8. The broker checks provenance, disk/RAM, the full effective Compose, live database history and recovery. It creates a fresh dump, restores it in a separate networkless PostgreSQL container with 256 MiB RAM/0.5 CPU/limited tmpfs and retrieves the encrypted off-host copy to verify its exact bytes.
9. The installed controller activates only changed `api`, `worker`, `dashboard`, `auth`, `mcp`, and independently publishes changed `legal/` and `public-site/`. Static-only and documentation-only releases advance accepted release metadata/source without recreating applications. It never rebuilds on staging or recreates Caddy, databases or other products. Startup migration is false; automatic mode preserves model and credential files.

### Public content and release identity

The signed manifest catalogs every public file by SHA256 and derives `Mobile__AndroidVersion` as `versionName+versionCode` from the exact archived Gradle source. Verification recomputes both. A release journals both prior public trees, `shared/release.env`, public metadata and their checksums before any publication. The root-owned `legal` and `public` bind directories keep their inode, so the existing Caddy container sees changes. Files are replaced atomically; additions, removals and file/directory shape changes are supported. The complete release is accepted only after both catalogs and all health checks pass. Multi-file publication can briefly expose a mixed set while the transaction runs; failure/interruption restores both complete prior trees. This does not claim a single atomic filesystem switch across Caddy's existing separate bind mounts.

`Deploy__ReleaseSha` and `Deploy__LastDeployAt` in `shared/release.env` identify the last accepted release and its UTC acceptance time. `Backend__Version` identifies the actual running API source (12-character SHA), which stays unchanged for static/frontend/documentation-only releases. Android metadata describes source, not a published store build. All other environment lines, including credentials, are preserved byte for byte. Rollback restores the exact prior file bytes and public metadata, rather than calculating a new acceptance time. If protected credential lines changed after the release, recovery stops before any mutation and requires operator reconciliation; it cannot silently overwrite a later credential rotation.

The API reads the controller's bounded public `shared/release-metadata/accepted.json` on every admin health request through a read-only directory bind at `/run/convy-release`. The public schema also records the full `backendSourceSha` and `backendDeployedAtUtc` at acceptance. The directory bind keeps atomic file replacements visible without API restarts. The API validates the public schema; absent/invalid metadata falls back to runtime configuration. Its environment's SHA/time remain the API deployment identity, whereas the admin release SHA/time describe acceptance. During a candidate's health checks, the last accepted metadata can still describe the prior backend, while `Backend__Version` already identifies the running candidate. No secret is included in the public JSON.

One-time administrator setup must create the root-owned non-writable `shared/release-metadata` directory (0755), ensure private `shared/release.env` (0600) is included in the ordered profile env files but excluded from credential preservation files, and pin the existing root-owned `legal`/`public` directories and exact Caddy read-only binds. Installing the reviewed `release_content.py` alongside the fixed controller/verifier is required. The first API activation adds only the public metadata directory mount; Caddy remains untouched. These operations are not performed by source review or merge.

Static source is limited to 64 MiB/4096 entries; each prior host tree has the same limit. Capacity reserves an additional 336 MiB for both prior trees, bounded archive overhead, temporary rollback extraction and the largest atomic file replacement, plus source extraction. Journals follow the existing current/previous/failure retention policy. Symlinks, special files, archive traversal, duplicate entries and unexpected bind roots fail closed. Host writers must respect the common lock. Managed release env files must have a final newline, UTF-8 without BOM and no duplicate metadata keys; planning rejects unsupported input before activation.
10. Bounded health and root-reviewed regression commands run before the current pointer/accepted ledger changes. Failures restore affected application images/configuration. Cleanup and a result record follow acceptance. A watchdog can recover an interrupted journal without starting a new release.

Converso's future CD, backup and maintenance writers must use **the same lock inode/path**, root ownership and nonblocking `flock` contract for their entire resource-consuming transaction. Separate per-product state directories remain independent. Existing writers that bypass this contract must be reconciled during first activation; this source does not change Converso.

## Disk, memory and retention policy

The private read-only inspection accounts separately for images, builder cache, container logs, Convy releases and legacy recovery archives. The policy below is derived from those measurements and the prior reviewed reserve. Image layer sizes must not be double-counted as physical daemon usage.

| Policy | Initial reviewed envelope and rationale |
| --- | --- |
| Minimum free disk | 8 GiB, retaining the prior reviewed controller's reserve: approximately one fifth of the filesystem; actual free space is rechecked on every release |
| Minimum available memory | 1 GiB: 512 MiB largest application envelope plus 512 MiB shared-host safety headroom; actual available memory is rechecked on every release |
| Incoming cap | 2 GiB per immutable bundle; bounded upload protocol; actual final artifact must be measured again at first activation |
| Managed release material | 3 GiB maximum, derived from private measurements of existing release/recovery materials with roughly twofold room; a budget check is mandatory, not an assumption that every future image fits |
| Prior image archive | 1 GiB maximum, more than twice the measured historical recovery archive; reserve the full envelope before snapshotting and reject an oversized archive before activation |
| Success retention | Current and one designated previous accepted journal, including prior source and exact checksummed candidate/rollback archives |
| Failed retention | At most two rolled-back transactions, for at most 24 hours; optional debug material can be reclaimed earlier under capacity pressure. Current/designated rollback/unresolved recovery are always protected |
| Aborted transfer | One lease at a time; abandoned CD-owned incoming directories removed under that lease before the next bounded upload |
| CI cache | Historical 4 GB cap on the run's dedicated builder; no global cache pruning; remove that builder at exit |
| Release DB recovery | Two fresh verified CD dumps; separate from existing daily 7-day, weekly 35-day and monthly 120-day retention; image cleanup cannot reach backups |
| Encrypted copies | Dedicated `convy-cd` restic snapshots: last two, daily seven, weekly five, monthly four; preserve other snapshot groups |

Before transfer, loading and application changes, budget reserve + incoming bytes + conservative expanded images + prior image archive + source extraction and backup work. Recorded physical usage includes daemon images/cache, releases, incoming, container logs and backups. Failure to fit stops safely. The source's caps are deliberately checked against real sizes; first activation must recalibrate/reapprove if the final full artifact or database falls outside the measured envelope.

Engine image `Size` is not assumed to equal containerd disk cost. Verification streams unique archive layers, checks compressed versus expanded bytes and budgets stored content plus twice expanded bytes for snapshot/extraction overhead. The signed manifest estimate is recomputed and compared on the host. A 16 GiB expansion ceiling stops an artifact that cannot fit the measured staging envelope; the stricter actual free-space reserve gate still applies.

The proposed application profile caps API at 512 MiB/one CPU and worker/web services at 256 MiB/half a CPU each. Private per-service readings informed these envelopes; the existing Convy configuration has no resource limits. These envelopes require activation/load-test review; they are not inferred workload peaks. Only selected Convy applications receive `json-file` rotation, 10 MB times three files (about 150 MB across five services). Shared edge/databases/other products remain unchanged. Their existing unbounded logs remain measured capacity risks and need independent review.

Only image IDs in the CD ownership ledger may be removed. The controller rechecks **all products' containers, including stopped containers**, and both protected journals before each removal. A failed image deletion stays in the ledger for retry. It never runs daemon-wide image/system/volume prune, never deletes a database backup through release cleanup, and never removes unknown legacy build cache. Legacy rollback/backup directories stay outside managed roots. Growing unowned cache, oversized logs or protected materials cause capacity failure and an operator alert, not unsafe deletion. Container log rotation and any legacy cache remediation require their own first-activation review; active logs are never truncated by this controller. See [Docker prune semantics](https://docs.docker.com/engine/manage-resources/pruning/).

## Database and backup policy

| Change class | Required behavior |
| --- | --- |
| No schema change | Compare complete migration catalog and actual applied IDs; fresh isolated restore + encrypted retrieval; application-only release; keep startup migration off |
| Verified backward-compatible additive | Separate schema compatibility review must establish old/current/rollback application read/write tests against the expanded schema, bounded SQL execution and an additive-only contract. Apply the reviewed database operation under the shared lease, then advance the pinned schema baseline/catalog. Subsequent ordinary application commits deploy automatically. A migration filename or “additive” label alone never qualifies |
| Destructive/incompatible/unknown | Block automatic application activation; require a maintenance/recovery plan, off-host recovery test and explicit operator decision. Never automatically downgrade or replay DB schema during application rollback |

This first source implementation intentionally automates the **unchanged-schema** path. It does not claim an implemented arbitrary EF-migration executor. New migration catalogs produce a specific compatibility gate; additive migrations have the reviewed expand/test/advance-baseline path above, rather than permanent rejection or uncontrolled `MigrateOnStartup`. Automating additive schema execution requires the compatibility certificate/executor and corresponding old/new image tests in a subsequent reviewed change.

Existing backup scripts contain daily/weekly/monthly buckets, `pg_restore --list`, optional restic export, backup-run recording and separate restore verification. The live scheduled timers are missing; a manually verified encrypted backup is not scheduled recovery. Keep the existing bucket policy. During activation review, freeze scheduled tools outside `current`, wrap backup/restore/retention with the shared lease, connect timer `OnFailure` and watchdog failures to the existing operator alert transport, verify daily 03:15 and Sunday 04:20 host-time schedules and randomized delays, and test missed/failed runs. **Do not enable timers in this source iteration.**

Each automatic release creates an actual isolated restore and verifies retrieval of that exact encrypted dump before proceeding. The private database measurement fits this first restore envelope, which caps the database at 64 MiB with 256 MiB bounded tmpfs/container memory and budgets four database-size copies before backup work. Growth outside that envelope produces an explicit resource/recovery gate, requiring a reviewed larger restore design. `offsite-export.py` requires a root-private reviewed endpoint/password file; no provider key is present in source or CI artifacts. Tests use an encrypted local restic repository and a separate PostgreSQL container: they prove the source behavior, not that real off-host connectivity/timers/alerts are operational. First activation must test the real endpoint and alert transport.

## Failure and recovery matrix

| Failure | Result |
| --- | --- |
| Failed/PR/fork/superseded CI, wrong signer/ref/SHA | No deployment; fail before privileged activation |
| Duplicate accepted source | No container recreation; accepted ledger cannot move backwards |
| Shared host busy | No resource-consuming transaction; bounded workflow failure/retry later |
| Missing/corrupt/oversized/partial artifact | No image load or application activation; abandoned bounded transfer reclaimed next lease |
| RAM/disk/retention insufficient | Stop before load/apply; only safe owned housekeeping permitted |
| Effective Compose/override/credential/shared topology drift | Compatibility exception; no application changes |
| Backup, isolated restore, encrypted retrieval or DB compatibility fails | No activation; previous accepted release remains |
| Load/start/health/regression failure | Restore only affected application services and exact environment/pointer/CI ledger; shared services preserved |
| Interrupted activation | Root journal recovery under the shared lease, next broker or five-minute watchdog |
| Rollback fails or database changed | Preserve all recovery materials; `RECOVERY_REQUIRED`; alert/operator intervention, no cleanup of that journal |
| Housekeeping failure after acceptance | Accepted application remains; recorded maintenance failure/workflow failure; ownership ledger preserves retry candidates |

GitHub Actions failures are visible release alerts; operator notification and systemd alert transport must be verified at activation. The broker's stdout/error codes withhold subprocess output and secrets; root-private journals contain configuration needed for recovery and must not be uploaded to public Actions artifacts.

GitHub Pages is independent expected documentation publication. Android keeps its version-file, successful master-CI, `android-release` environment and credential-cleanup safeguards. Neither publication permits a VPS change in this inactive source phase.

## First activation checklist (separate approval)

1. Approve the exact integration PR head after all CI, security, runtime, Docker and review evidence. Authorize its merge separately; PR #32/#34 are not merged by this source iteration.
2. Verify the old `Backend Staging Release` workflow remains disabled and queued legacy deploy runs are absent.
3. Measure the exact full artifact, runtime closure, image expansion, current/rollback/archive sizes, live limits/log rotation and disk/RAM again. Confirm the 8 GiB/1 GiB reserves and retention envelope can accommodate both products.
4. Pin all ordered live Compose/environment files and hashes, existing mount/network/volume identities, schema baseline/catalog/history, root-owned protected files and regression acceptance commands. Preserve credentials and current model configuration.
5. Review/install frozen tool bytes outside `current`; root owns all parent directories/files. Install fixed wrappers with a cleared environment. No source from an incoming artifact is executable.
6. Provision `convy-cd` without Docker-group membership or a shell deployment path. Use authorized-key `restrict,command="sudo -n /usr/local/libexec/convy-staging-broker"`; sudoers allows exactly that fixed command **with no arguments**, `env_reset` and no `SETENV`. Test shell/forwarding/alternate command denial.
7. Configure a least-privilege read-only GitHub API token on the host; verify `gh` supports the exact certificate/source/signer flags. Preserve the independently verified SSH server pin; review the dedicated staging-only key.
8. Approve the actual encrypted off-host endpoint and credentials. Run a fresh isolated restore and encrypted retrieval; verify backup capacity and bounded retention. Reconcile historical backups without image cleanup touching them.
9. Reconcile every product's resource writer with the common host lease. Prepare/watchdog and backup/restore schedules plus real failure alert routing; enabling each timer is a separately authorized operational step.
10. Preserve GitHub `staging` master-only boundary. Review first-activation exception protection; normal subsequent compatible staging deployments must not require per-release human approval. Populate new scoped variables/secrets only in the authorized rollout phase.
11. Create root profile with `modelPolicy: preserve`, reviewed resource caps and `automaticEnabled: false`; verify rejection. Test a dry run, health regression failure, interruption and independent rollback on the actual host under controlled rollout approval.
12. Only after approval set the root activation flag and `STAGING_CD_ENABLED=true`. Leave old workflow disabled. Verify one controlled successful master CD and its measured retention/rollback results before declaring live CD operational.

Source review/testing status and private command evidence belong in the iteration report. `STAGING_CD_SOURCE_READY` means reviewed/tested inactive source; it does not mean staging CD is enabled.
