import { readFile, mkdir, writeFile } from "node:fs/promises";
import { spawnSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const effects = { braces: [], micromatch: ["braces"], "fast-glob": ["micromatch"], chokidar: ["braces"], tailwindcss: ["chokidar", "fast-glob", "micromatch"], "vite-plugin-singlefile": ["micromatch"] };
export function parseUpstream(record, registry) {
  const entries = record.vulnerabilities?.filter(v => v.package?.ecosystem === "npm" && v.package?.name === "braces");
  if (record.ghsa_id !== "GHSA-vfj7-8cjw-p6xm" || !entries?.length || typeof registry.version !== "string"
    || entries.some(v => !Object.hasOwn(v, "first_patched_version") || (v.first_patched_version !== null && typeof v.first_patched_version?.identifier !== "string"))) throw Error("Invalid upstream advisory");
  return { latest: registry.version, patched: entries.some(v => v.first_patched_version !== null) };
}
export function verifyAudit(report, lock, app, upstream) {
  if (report.auditReportVersion !== 2 || !report.vulnerabilities || report.error || !["dashboard", "auth", "mcp"].includes(app)) throw Error("Invalid audit response/application");
  const findings = Object.entries(report.vulnerabilities);
  if (findings.some(([, v]) => !["info", "low", "moderate", "high", "critical"].includes(v.severity))) throw Error("Unknown audit severity");
  const high = findings.filter(([, v]) => ["high", "critical"].includes(v.severity));
  if (!high.length) return [];
  if (upstream.latest !== "3.0.3" || upstream.patched) throw Error("Review braces exception after upstream change");
  const allowed = app === "mcp" ? ["braces", "micromatch", "vite-plugin-singlefile"] : ["braces", "micromatch", "fast-glob", "chokidar", "tailwindcss"];
  const reviewed = app === "mcp" ? [
    ["", "devDependencies", "vite-plugin-singlefile", "2.3.3"],
    ["node_modules/vite-plugin-singlefile", "dependencies", "micromatch", "^4.0.8"],
    ["node_modules/micromatch", "dependencies", "braces", "^3.0.3"],
  ] : [
    ["", "devDependencies", "tailwindcss", "3.4.19"],
    ["node_modules/tailwindcss", "dependencies", "chokidar", "^3.6.0"],
    ["node_modules/tailwindcss", "dependencies", "fast-glob", "^3.3.2"],
    ["node_modules/tailwindcss", "dependencies", "micromatch", "^4.0.8"],
    ["node_modules/chokidar", "dependencies", "braces", "~3.0.2"],
    ["node_modules/fast-glob", "dependencies", "micromatch", "^4.0.8"],
    ["node_modules/micromatch", "dependencies", "braces", "^3.0.3"],
  ];
  const edges = [];
  for (const [node, pkg] of Object.entries(lock.packages)) {
    for (const field of ["dependencies", "devDependencies", "optionalDependencies", "peerDependencies"]) {
      for (const [name, version] of Object.entries(pkg[field] ?? {})) {
        if (allowed.includes(name)) edges.push(JSON.stringify([node, field, name, version]));
      }
    }
  }
  if (JSON.stringify(edges.sort()) !== JSON.stringify(reviewed.map(edge => JSON.stringify(edge)).sort())) throw Error("Unreviewed braces dependency boundary");
  for (const [name, finding] of high) {
    if (!allowed.includes(name) || finding.severity !== "high" || !finding.nodes?.length) throw Error(`Release-blocking advisory: ${name}`);
    for (const node of finding.nodes) if (lock.packages[node]?.dev !== true) throw Error(`Finding reaches production: ${node}`);
    if (name === "braces") {
      if (finding.via.length !== 1 || finding.via[0].url !== "https://github.com/advisories/GHSA-vfj7-8cjw-p6xm" || finding.via[0].range !== "<=3.0.3" || finding.via[0].severity !== "high") throw Error("Unreviewed braces advisory");
    } else if (JSON.stringify([...finding.via].sort()) !== JSON.stringify([...effects[name]].sort())) throw Error(`Unreviewed effects: ${name}`);
  }
  if (lock.packages["node_modules/braces"]?.version !== "3.0.3") throw Error("Unreviewed braces version");
  const parent = app === "mcp" ? "vite-plugin-singlefile" : "tailwindcss";
  const version = app === "mcp" ? "2.3.3" : "3.4.19";
  if (lock.packages[""].devDependencies[parent] !== version) throw Error("Unreviewed build-tool parent");
  return high.map(([name]) => name);
}
async function main(app) {
  if (!["dashboard", "auth", "mcp"].includes(app) || !process.env.npm_execpath) throw Error("Run the application's security:audit npm script");
  const directory = path.join(root, app), output = path.join(root, "artifacts/dependency-audit", app);
  await mkdir(output, { recursive: true });
  async function audit(label, args) {
    const result = spawnSync(process.execPath, [process.env.npm_execpath, "audit", ...args, "--json"], { cwd: directory, encoding: "utf8" });
    await writeFile(path.join(output, `${label}.json`), result.stdout || "");
    if (result.error || ![0, 1].includes(result.status)) throw Error("Audit execution failed");
    const report = JSON.parse(result.stdout);
    if (report.auditReportVersion !== 2 || !report.vulnerabilities || report.error) throw Error("Invalid audit response");
    return { report, status: result.status };
  }
  const production = await audit("production", ["--omit=dev", "--audit-level=high"]), full = await audit("full", []);
  if (production.status !== 0) throw Error("Production dependency audit blocks release");
  let upstream = { latest: null, patched: false };
  if (Object.values(full.report.vulnerabilities).some(v => ["high", "critical"].includes(v.severity))) {
    const records = await Promise.all(["https://api.github.com/advisories/GHSA-vfj7-8cjw-p6xm", "https://registry.npmjs.org/braces/latest"].map(async url => {
      const response = await fetch(url, { signal: AbortSignal.timeout(20_000), headers: { "User-Agent": "Convy-dependency-policy" } });
      if (!response.ok) throw Error("Upstream review unavailable"); return response.json();
    }));
    upstream = parseUpstream(records[0], records[1]);
    await writeFile(path.join(output, "upstream.json"), JSON.stringify(records, null, 2));
  }
  const lock = JSON.parse(await readFile(path.join(directory, "package-lock.json"), "utf8"));
  const accepted = verifyAudit(full.report, lock, app, upstream);
  await writeFile(path.join(output, "policy.json"), JSON.stringify({ accepted, threshold: "high", fullCounts: full.report.metadata.vulnerabilities }, null, 2));
  if (accepted.length) console.warn(`::warning::${app}: unpatched GHSA-vfj7-8cjw-p6xm in dev tooling only; final-image absence is a separate mandatory Infrastructure Config gate.`);
  console.log(`${app}: production audit and full high-severity policy passed; all findings retained`);
}
if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) await main(process.argv[2]);
