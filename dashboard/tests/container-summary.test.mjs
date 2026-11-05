import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { createRequire } from "node:module";
const require = createRequire(import.meta.url), ts = require("typescript");
const source = readFileSync(new URL("../lib/container-summary.ts", import.meta.url), "utf8");
const module = { exports: {} };
new Function("module", "exports", ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText)(module, module.exports);
const { summarizeContainers: summary, serviceNames: names } = module.exports;
const now = Date.parse("2026-11-05T08:00:00Z");
const fixture = () => ({ schemaVersion: 1, scope: "container", product: "convy", status: "available", capturedAtUtc: new Date(now).toISOString(), services: names.map((containerName, i) => ({ containerName, containerId: (i + 1).toString(16).repeat(64), state: "running", health: i === 0 ? "healthy" : i === 1 ? "unhealthy" : "unknown", cpuPercent: i === 0 ? 150 : 0, memoryBytes: 2 ** 20, restartCount: i === 0 ? 2 : 0 })) });
test("complete current capture sums product resources, including CPU above 100 and real zeros", () => {
  const result = summary(fixture(), now);
  assert.equal(result.expected, names.length); assert.equal(result.running, names.length);
  assert.deepEqual(result.cpu, { value: 150, count: names.length });
  assert.deepEqual(result.memory, { value: names.length * 2 ** 20, count: names.length });
  assert.deepEqual([result.healthy, result.unhealthy, result.unknown], [1, 1, names.length - 2]);
  assert.equal(result.restarts, 2);
});
test("partial values, stopped containers and impossible numbers expose separate coverage", () => {
  const data = fixture(); data.services[0].cpuPercent = null; data.services[1].memoryBytes = -1; data.services[2].state = "exited"; data.services[3].cpuPercent = Infinity;
  const result = summary(data, now);
  assert.equal(result.cpu.count, names.length - 3); assert.equal(result.cpu.value, 0);
  assert.equal(result.memory.count, names.length - 2); assert.equal(result.running, names.length - 1);
  assert.equal(summary({ ...data, services: [] }, now).memory.value, null);
});
test("ambiguous names or IDs and foreign product rows cannot inflate totals", () => {
  let data = fixture(); data.services[1].containerId = data.services[0].containerId;
  assert.equal(summary(data, now).identified, names.length - 2);
  data = fixture(); data.services.push({ ...data.services[0], containerId: "f".repeat(64) });
  assert.equal(summary(data, now).identified, names.length - 1);
  data = fixture(); data.services.push({ ...data.services[0], containerName: "foreign-api", containerId: "f".repeat(64), cpuPercent: 99999 });
  assert.equal(summary(data, now).cpu.value, 150);
});
test("missing, stale, future, wrong schema/scope/product and failed refresh never produce current totals", () => {
  const data = fixture();
  for (const invalid of [null, { ...data, status: "stale" }, { ...data, schemaVersion: 2 }, { ...data, scope: "host" }, { ...data, product: "foreign" }, { ...data, capturedAtUtc: "invalid" }, { ...data, capturedAtUtc: new Date(now - 180001).toISOString() }, { ...data, capturedAtUtc: new Date(now + 30001).toISOString() }]) {
    const result = summary(invalid, now); assert.equal(result.current, false); assert.equal(result.cpu.value, null); assert.equal(result.unknown, names.length);
  }
  assert.equal(summary(data, now, true).memory.value, null);
});
