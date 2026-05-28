import assert from "node:assert/strict";
import { test } from "node:test";

import { createProtectedResourceMetadata, readOnlyScopes, supportedScopes, writeScopes } from "../src/metadata.js";

test("protected resource metadata advertises read and limited write Convy scopes", () => {
  const metadata = createProtectedResourceMetadata({
    mcpPublicUrl: "https://mcp.convy.app",
    authPublicUrl: "https://auth.convy.app",
  });

  assert.equal(metadata.resource, "https://mcp.convy.app");
  assert.deepEqual(metadata.authorization_servers, ["https://auth.convy.app"]);
  assert.deepEqual(metadata.scopes_supported, supportedScopes);
  assert.ok(writeScopes.every((scope) => metadata.scopes_supported.includes(scope)));
  assert.ok(!(metadata.scopes_supported as readonly string[]).includes("convy.lists.write"));
});
