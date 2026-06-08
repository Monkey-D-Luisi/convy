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

    assert.match(source, /buildAdminApiTarget/);
    assert.match(source, /encodeURIComponent/);
    assert.match(source, /startsWith\("\/api\/v1\/admin\/"\)/);
    assert.match(source, /target\.search = request\.nextUrl\.search/);
    assert.match(source, /authorization:\s*`Bearer \$\{firebaseToken\}`/);
    assert.match(source, /cache:\s*"no-store"/);
    assert.match(source, /headers\.set\("cache-control",\s*"no-store"\)/);
    assert.match(source, /headers\.set\("content-type"/);
    assert.match(source, /headers\.set\("content-disposition"/);
    assert.match(source, /headers\.set\("content-length"/);
    assert.match(source, /status:\s*response\.status/);
  });

  it("rejects path traversal and encoded separators before proxying", async () => {
    const source = await readDashboardFile("app", "api", "admin", "[...path]", "route.ts");

    assert.match(source, /rejectUnsafeAdminPathSegment/);
    assert.match(source, /decodeURIComponent/);
    assert.match(source, /decoded === "\."/);
    assert.match(source, /decoded === "\.\."/);
    assert.match(source, /includes\("\\\\"\)/);
    assert.match(source, /includes\("\/"\)/);
    assert.match(source, /return new Response\(null,\s*\{\s*status:\s*400\s*\}\)/s);
  });
});

describe("admin auth security contract", () => {
  it("uses non-persistent Firebase admin auth sessions", async () => {
    const source = await readDashboardFile("lib", "firebase.ts");

    assert.match(source, /inMemoryPersistence/);
    assert.match(source, /setPersistence\(auth,\s*inMemoryPersistence\)/);
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

  it("loads charting code outside the initial admin view bundle", async () => {
    const source = await readDashboardFile("components", "admin-views.tsx");
    const charts = await readDashboardFile("components", "admin-charts.tsx");

    assert.match(source, /dynamic\(\(\) => import\("@\/components\/admin-charts"\)/);
    assert.doesNotMatch(source, /from "recharts"/);
    assert.match(charts, /from "recharts"/);
  });

  it("makes charts readable without hover and guards against zero-size containers", async () => {
    const charts = await readDashboardFile("components", "admin-charts.tsx");
    const source = await readDashboardFile("components", "admin-views.tsx");

    assert.match(charts, /Legend/);
    assert.match(charts, /height=\{280\}/);
    assert.match(charts, /isAnimationActive=\{false\}/);
    assert.match(charts, /ChartPalette/);
    assert.match(source, /min-h-\[18rem\]/);
    assert.match(source, /SystemTrendsChart/);
  });

  it("uses URL-backed reporting ranges for dashboard metric views", async () => {
    const source = await readDashboardFile("components", "admin-views.tsx");

    assert.match(source, /useSearchParams/);
    assert.match(source, /useRouter/);
    assert.match(source, /URLSearchParams/);
    assert.match(source, /function RangeControl/);
    assert.match(source, /setRangeDays/);
  });

  it("provides risk-first overview and mobile alternatives for dense tables", async () => {
    const source = await readDashboardFile("components", "admin-views.tsx");

    assert.match(source, /Operations Risk/);
    assert.match(source, /Action Needed/);
    assert.match(source, /RiskItemList/);
    assert.match(source, /function ResponsiveDataTable/);
    assert.match(source, /md:hidden/);
    assert.match(source, /hidden md:block/);
  });

  it("distinguishes no-data rate states from zero-percent outcomes", async () => {
    const source = await readDashboardFile("components", "admin-views.tsx");

    assert.match(source, /function formatRatioWithDenominator/);
    assert.match(source, /formatRatioWithDenominator\(data\.voiceReliability\.successRate7d,\s*data\.voiceReliability\.requests7d,\s*"No voice requests"\)/);
    assert.match(source, /formatRatioWithDenominator\(data\.mcp\.successRate24h,\s*data\.mcp\.toolCalls24h,\s*"No MCP calls"\)/);
    assert.match(source, /formatRatioWithDenominator\(overview\.usage\.successRate,\s*overview\.usage\.invocations,\s*"No MCP calls"\)/);
    assert.doesNotMatch(source, /Voice success rate 7d" value=\{formatRatio\(data\.voiceReliability\.successRate7d\)\}/);
  });

  it("puts backup posture before backup run audit rows", async () => {
    const source = await readDashboardFile("components", "admin-views.tsx");

    assert.match(source, /function getBackupPosture/);
    assert.match(source, /function BackupPostureSummary/);
    assert.match(source, /<BackupPostureSummary backups=\{data\} \/>/);
    assert.match(source, /Latest verified/);
    assert.match(source, /Last failure/);
    assert.match(source, /Posture/);
    assert.match(source, /Attention needed/);
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
