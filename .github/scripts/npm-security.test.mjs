import test from "node:test";
import assert from "node:assert/strict";
import { verifyAudit, parseUpstream } from "./npm-security.mjs";
import { verifyRuntimeTree } from "./runtime-dependencies.mjs";
import { readFile, mkdtemp, mkdir, writeFile, symlink, rm } from "node:fs/promises";
import os from "node:os";
import path from "node:path";
function fixture() {
  return [{ auditReportVersion: 2, vulnerabilities: { braces: { severity: "high", nodes: ["node_modules/braces"], via: [{ url: "https://github.com/advisories/GHSA-vfj7-8cjw-p6xm", range: "<=3.0.3", severity: "high" }] } } }, { packages: {
    "": { devDependencies: { tailwindcss: "3.4.19" } }, "node_modules/braces": { dev: true, version: "3.0.3" },
    "node_modules/tailwindcss": { dependencies: { chokidar: "^3.6.0", "fast-glob": "^3.3.2", micromatch: "^4.0.8" } },
    "node_modules/chokidar": { dependencies: { braces: "~3.0.2" } },
    "node_modules/fast-glob": { dependencies: { micromatch: "^4.0.8" } },
    "node_modules/micromatch": { dependencies: { braces: "^3.0.3" } },
  } }];
}
test("only reviewed dev-only braces can be non-blocking", () => {
  const [report, lock] = fixture();
  assert.deepEqual(verifyAudit(report, lock, "dashboard", { latest: "3.0.3", patched: false }), ["braces"]);
  lock.packages["node_modules/braces"].dev = false;
  assert.throws(() => verifyAudit(report, lock, "dashboard", { latest: "3.0.3", patched: false }), /production/);
});
test("additional usage of the exempted chain blocks every application", async () => {
  for (const app of ["dashboard", "auth", "mcp"]) {
    const [report] = fixture();
    const lock = JSON.parse(await readFile(new URL(`../../${app}/package-lock.json`, import.meta.url), "utf8"));
    const upstream = { latest: "3.0.3", patched: false };
    assert.deepEqual(verifyAudit(report, lock, app, upstream), ["braces"]);
    lock.packages[""].devDependencies.braces = "3.0.3";
    assert.throws(() => verifyAudit(report, lock, app, upstream), /boundary/);
  }
});
test("missing upstream patch metadata fails closed", () => {
  const record = { ghsa_id: "GHSA-vfj7-8cjw-p6xm", vulnerabilities: [{ package: { ecosystem: "npm", name: "braces" }, first_patched_version: null }] };
  assert.equal(parseUpstream(record, { version: "3.0.3" }).patched, false);
  delete record.vulnerabilities[0].first_patched_version;
  assert.throws(() => parseUpstream(record, { version: "3.0.3" }), /Invalid/);
});
test("runtime inspection follows links and rejects vulnerable linked packages", async () => {
  const root = await mkdtemp(path.join(os.tmpdir(), "convy-links-"));
  try {
    await mkdir(path.join(root, "node_modules")); await mkdir(path.join(root, "vendored"));
    await writeFile(path.join(root, "package.json"), '{"name":"convy"}');
    await writeFile(path.join(root, "vendored", "package.json"), '{"name":"braces","version":"3.0.3"}');
    await symlink(path.join(root, "vendored"), path.join(root, "node_modules", "linked"), process.platform === "win32" ? "junction" : "dir");
    await assert.rejects(verifyRuntimeTree(root), /Development-only/);
  } finally { await rm(root, { recursive: true }); }
});
test("unknown high, critical, changed upstream and malformed output block", () => {
  const [report, lock] = fixture(); report.vulnerabilities.hono = { severity: "high" };
  assert.throws(() => verifyAudit(report, lock, "dashboard", { latest: "3.0.3", patched: false }), /Release-blocking/);
  delete report.vulnerabilities.hono; report.vulnerabilities.braces.severity = "critical";
  assert.throws(() => verifyAudit(report, lock, "dashboard", { latest: "3.0.3", patched: false }), /Release-blocking/);
  assert.throws(() => verifyAudit(report, lock, "dashboard", { latest: "3.0.4", patched: false }), /upstream/);
  assert.throws(() => verifyAudit({}, lock, "dashboard", {}), /Invalid/);
});
