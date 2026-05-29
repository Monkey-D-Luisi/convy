import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";

import { createToolDescriptorMetadata } from "../src/tools/server.js";
import { toolDefinitions } from "../src/tools/definitions.js";
import {
  CONVY_WIDGET_CSP,
  CONVY_WIDGET_DOMAIN,
  CONVY_WIDGET_RESOURCE_URI,
} from "../src/widget-metadata.js";

test("widget metadata is submission-ready for ChatGPT Apps review", () => {
  assert.equal(CONVY_WIDGET_RESOURCE_URI, "ui://widget/convy-summary-v1.html");
  assert.equal(CONVY_WIDGET_DOMAIN, "https://mcp.convyapp.com");
  assert.deepEqual(CONVY_WIDGET_CSP, {
    connectDomains: [],
    resourceDomains: [],
    frameDomains: [],
  });
});

test("widget resource declares standard and compatibility domain metadata", () => {
  const serverSource = readFileSync(new URL("../src/tools/server.ts", import.meta.url), "utf8");

  assert.match(serverSource, /domain: CONVY_WIDGET_DOMAIN/);
  assert.match(serverSource, /"openai\/widgetDomain": CONVY_WIDGET_DOMAIN/);
});

test("widget HTML is cached after the first filesystem lookup", () => {
  const serverSource = readFileSync(new URL("../src/tools/server.ts", import.meta.url), "utf8");

  assert.match(serverSource, /let cachedWidgetHtml: string \| null = null/);
  assert.match(serverSource, /if \(cachedWidgetHtml !== null\)/);
  assert.match(serverSource, /cachedWidgetHtml = readFileSync\(existing, "utf8"\)/);
  assert.match(serverSource, /cachedWidgetHtml = \[/);
});

test("MCP-hosted widget origin has documented Apps review isolation rationale", () => {
  const readinessAudit = readFileSync(
    new URL("../../docs/mcp/submission-readiness-audit.md", import.meta.url),
    "utf8",
  );

  assert.match(readinessAudit, /https:\/\/mcp\.convyapp\.com/);
  assert.match(readinessAudit, /widget isolation/i);
  assert.match(readinessAudit, /no cookies/i);
  assert.match(readinessAudit, /empty connect, resource, and frame domains/i);
});

test("every MCP tool references the shared widget and has explicit status text", () => {
  for (const definition of toolDefinitions) {
    const metadata = createToolDescriptorMetadata(definition);

    assert.equal(metadata._meta.ui.resourceUri, CONVY_WIDGET_RESOURCE_URI);
    assert.equal(metadata._meta["openai/outputTemplate"], CONVY_WIDGET_RESOURCE_URI);
    assert.match(metadata._meta["openai/toolInvocation/invoking"], /^Checking|^Updating|^Adding|^Loading/);
    assert.match(metadata._meta["openai/toolInvocation/invoked"], /Convy/);

    if (definition.annotations.readOnlyHint) {
      assert.deepEqual(metadata._meta.ui.visibility, ["model", "app"]);
    } else {
      assert.deepEqual(metadata._meta.ui.visibility, ["model"]);
    }
  }
});

test("MCP package builds a React single-file Apps SDK widget", () => {
  const packageJson = JSON.parse(readFileSync(new URL("../package.json", import.meta.url), "utf8"));

  assert.equal(packageJson.dependencies["@modelcontextprotocol/ext-apps"], "1.7.2");
  assert.equal(packageJson.dependencies.react, "19.2.6");
  assert.equal(packageJson.dependencies["react-dom"], "19.2.6");
  assert.equal(packageJson.devDependencies.vite, "8.0.14");
  assert.equal(packageJson.devDependencies["@vitejs/plugin-react"], "6.0.2");
  assert.equal(packageJson.devDependencies["vite-plugin-singlefile"], "2.3.3");
  assert.equal(packageJson.scripts["build:widget"], "vite build --config widget/vite.config.ts");
  assert.match(packageJson.scripts.build, /build:widget/);
});
