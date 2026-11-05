import assert from "node:assert/strict";
import test from "node:test";
import { createRequire } from "node:module";
import { readFileSync } from "node:fs";
const require = createRequire(import.meta.url);
const ts = require("typescript");
const { renderToStaticMarkup } = require("react-dom/server");
const { createElement } = require("react");
const Module = require("node:module");
require.extensions[".ts"] = (module, file) => module._compile(ts.transpileModule(readFileSync(file, "utf8"), { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText, file);
const filename = new URL("../components/container-services.tsx", import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1");
const component = new Module(filename);
component.filename = filename;
component.paths = require.resolve.paths("react");
component._compile(ts.transpileModule(readFileSync(new URL("../components/container-services.tsx", import.meta.url), "utf8"), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX }
}).outputText, filename);
const { ContainerServices } = component.exports;

test("renders all six services, real zero values, unknown health and container identity", () => {
  const html = renderToStaticMarkup(createElement(ContainerServices, { metrics: { status: "available", schemaVersion: 1, scope: "container", product: "convy", capturedAtUtc: new Date().toISOString(), services: [{ containerName: "convy-api", containerId: "a".repeat(64), state: "running", health: "unknown", cpuPercent: 0, memoryBytes: 1048576, restartCount: 0 }] } }));
  for (const value of ["API", "Worker", "Authentication", "Admin dashboard", "MCP", "PostgreSQL", "0.00%", "1.0 MiB", "aaaaaaaaaaaa", "Infrastructure Ops"]) assert.ok(html.includes(value), value);
  assert.ok(!html.includes("chatbot-api"));
});
test("stale and missing telemetry show unknown values instead of old resource numbers", () => {
  for (const metrics of [null, { status: "stale", services: [{ containerName: "convy-api", cpuPercent: 99, memoryBytes: 1048576, restartCount: 45 }] }]) {
    const html = renderToStaticMarkup(createElement(ContainerServices, { metrics }));
    assert.ok(html.includes("Unknown"));
    assert.ok(!html.includes("99.00%"));
    assert.ok(!html.includes("1.0 MiB"));
  }
});
test("history charts keep legacy host values separate from container series", () => {
  const source = readFileSync(new URL("../components/admin-charts.tsx", import.meta.url), "utf8");
  assert.match(source, /metricScope === "legacy-host"/);
  assert.match(source, /metricScope === "container"/);
  assert.match(source, /Convy container memory MiB/);
  assert.match(source, /connectNulls=\{false\}/);
});
