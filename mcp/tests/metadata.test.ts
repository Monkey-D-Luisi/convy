import assert from "node:assert/strict";
import { test } from "node:test";

import { createProtectedResourceMetadata, readOnlyScopes, supportedScopes, writeScopes } from "../src/metadata.js";

test("protected resource metadata advertises read and limited write Convy scopes", () => {
  const metadata = createProtectedResourceMetadata({
    mcpPublicUrl: "https://mcp.convyapp.com",
    authPublicUrl: "https://auth.convyapp.com",
  });

  assert.equal(metadata.resource, "https://mcp.convyapp.com");
  assert.deepEqual(metadata.authorization_servers, ["https://auth.convyapp.com"]);
  assert.deepEqual(metadata.scopes_supported, supportedScopes);
  assert.ok(writeScopes.every((scope) => metadata.scopes_supported.includes(scope)));
  assert.ok(!(metadata.scopes_supported as readonly string[]).includes("convy.lists.write"));
});
