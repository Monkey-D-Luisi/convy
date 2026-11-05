# Product resource summary acceptance

The summary consumes one current container-scoped Convy publication. CPU uses Docker's one-core convention; memory uses Docker working set in MiB. Coverage is separate for CPU, memory and restart counts. Duplicate identities/names, foreign scope/product, old/future captures, missing values and non-running resource samples cannot inflate totals. Unknown health includes missing healthchecks and unavailable identities. Restart counts describe each current container's lifetime.

The six-service table and existing business, API/dependency, backup and historical views are retained. Historical container and `legacy-host` series remain separate with gaps. No backend, schema, collector, auth or network change is required for this UI release.
