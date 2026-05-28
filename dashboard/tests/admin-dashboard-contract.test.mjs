import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import path from "node:path";
import { describe, it } from "node:test";

const dashboardRoot = path.resolve(import.meta.dirname, "..");

async function readDashboardFile(...segments) {
  return readFile(path.join(dashboardRoot, ...segments), "utf8");
}

describe("admin API proxy contract", () => {
  it("forwards admin paths, search params, Firebase token, and response metadata", async () => {
    const source = await readDashboardFile("app", "api", "admin", "[...path]", "route.ts");

    assert.match(source, /new URL\(`\/api\/v1\/admin\/\$\{path\.join\("\/"\)\}`,\s*apiBaseUrl\)/);
    assert.match(source, /target\.search = request\.nextUrl\.search/);
    assert.match(source, /authorization:\s*`Bearer \$\{firebaseToken\}`/);
    assert.match(source, /cache:\s*"no-store"/);
    assert.match(source, /headers\.set\("cache-control",\s*"no-store"\)/);
    assert.match(source, /headers\.set\("content-type"/);
    assert.match(source, /headers\.set\("content-disposition"/);
    assert.match(source, /headers\.set\("content-length"/);
    assert.match(source, /status:\s*response\.status/);
  });
});

describe("admin view UX contract", () => {
  it("keeps operational controls and empty states in every high-impact view", async () => {
    const source = await readDashboardFile("components", "admin-views.tsx");

    assert.match(source, /function ViewHeader/);
    assert.match(source, /Refresh/);
    assert.match(source, /lastUpdatedAt/);
    assert.match(source, /Range:/);
    assert.match(source, /function EmptyState/);
    assert.match(source, /No backup runs yet/);
    assert.match(source, /No AI operation rows/);
    assert.match(source, /overflow-x-auto/);
    assert.match(source, /max-w-\[14rem\]\s+truncate/);
    assert.match(source, /releaseSha\.slice\(0,\s*12\)/);
    assert.match(source, /export function McpView/);
    assert.match(source, /isMcpOverviewPayload/);
    assert.match(source, /Unexpected MCP admin payload/);
    assert.match(source, /Publication Readiness/);
    assert.match(source, /Recent Invocations/);
    assert.doesNotMatch(source, /tokenHash/);
    assert.doesNotMatch(source, /refreshToken:\s*string/);
  });

  it("keeps dashboard resources refreshable and non-cacheable", async () => {
    const source = await readDashboardFile("components", "admin-views.tsx");

    assert.match(source, /const \[reloadIndex,\s*setReloadIndex\]/);
    assert.match(source, /cache:\s*"no-store"/);
    assert.match(source, /refresh:\s*\(\) => setReloadIndex/);
    assert.match(source, /\[path,\s*reloadIndex,\s*token\]/);
  });
});

describe("MCP admin dashboard contract", () => {
  it("exposes the MCP route and navigation entry", async () => {
    const shell = await readDashboardFile("components", "admin-shell.tsx");
    const page = await readDashboardFile("app", "mcp", "page.tsx");

    assert.match(shell, /href:\s*"\/mcp"/);
    assert.match(shell, /label:\s*"MCP"/);
    assert.match(page, /McpView/);
  });

  it("types the MCP admin payload and overview/system summaries", async () => {
    const types = await readDashboardFile("lib", "types.ts");

    assert.match(types, /export type McpOverview/);
    assert.match(types, /export type McpRuntime/);
    assert.match(types, /export type McpPublicationReadinessCheck/);
    assert.match(types, /mcp:\s*McpSummary/);
    assert.match(types, /mcpHealthy:\s*boolean/);
    assert.match(types, /authMetadataHealthy:\s*boolean/);
    assert.doesNotMatch(types, /tokenHash/);
    assert.doesNotMatch(types, /authorizationCode:\s*string/);
  });
});
