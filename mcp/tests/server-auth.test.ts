import assert from "node:assert/strict";
import { generateKeyPairSync } from "node:crypto";
import type { AddressInfo } from "node:net";
import { test } from "node:test";
import { exportSPKI, SignJWT } from "jose";
import { createApp } from "../src/server.js";
import { supportedScopes } from "../src/metadata.js";

test("MCP endpoint returns OAuth challenge when token is missing", async () => {
  const { publicKey } = generateKeyPairSync("rsa", { modulusLength: 2048 });
  const app = createApp({
    port: 0,
    apiBaseUrl: "https://api.convy.app",
    mcpPublicUrl: "https://mcp.convy.app",
    authPublicUrl: "https://auth.convy.app",
    jwtIssuer: "https://auth.convy.app",
    jwtAudience: "https://mcp.convy.app",
    jwtPublicKeyPem: await exportSPKI(publicKey),
  });
  const server = app.listen(0);

  try {
    const port = (server.address() as AddressInfo).port;
    const response = await fetch(`http://127.0.0.1:${port}/mcp`, { method: "POST" });

    assert.equal(response.status, 401);
    assert.match(response.headers.get("www-authenticate") ?? "", /resource_metadata="https:\/\/mcp\.convy\.app\/\.well-known\/oauth-protected-resource"/);
  } finally {
    server.close();
  }
});

test("MCP endpoint rejects tokens missing Convy scopes", async () => {
  const { publicKey, privateKey } = generateKeyPairSync("rsa", { modulusLength: 2048 });
  const token = await new SignJWT({
    sub: "11111111-1111-4111-8111-111111111111",
    token_use: "mcp_access",
    scope: "profile",
  })
    .setProtectedHeader({ alg: "RS256" })
    .setIssuer("https://auth.convy.app")
    .setAudience("https://mcp.convy.app")
    .setExpirationTime("5m")
    .sign(privateKey);
  const app = createApp({
    port: 0,
    apiBaseUrl: "https://api.convy.app",
    mcpPublicUrl: "https://mcp.convy.app",
    authPublicUrl: "https://auth.convy.app",
    jwtIssuer: "https://auth.convy.app",
    jwtAudience: "https://mcp.convy.app",
    jwtPublicKeyPem: await exportSPKI(publicKey),
  });
  const server = app.listen(0);

  try {
    const port = (server.address() as AddressInfo).port;
    const response = await fetch(`http://127.0.0.1:${port}/mcp`, {
      method: "POST",
      headers: { authorization: `Bearer ${token}` },
    });

    assert.equal(response.status, 403);
    assert.equal((await response.json() as { error: string }).error, "insufficient_scope");
  } finally {
    server.close();
  }
});

test("MCP endpoint accepts tokens with any supported Convy scope", async () => {
  const { publicKey, privateKey } = generateKeyPairSync("rsa", { modulusLength: 2048 });
  const token = await new SignJWT({
    sub: "11111111-1111-4111-8111-111111111111",
    token_use: "mcp_access",
    scope: supportedScopes.at(-1),
  })
    .setProtectedHeader({ alg: "RS256" })
    .setIssuer("https://auth.convy.app")
    .setAudience("https://mcp.convy.app")
    .setExpirationTime("5m")
    .sign(privateKey);
  const app = createApp({
    port: 0,
    apiBaseUrl: "https://api.convy.app",
    mcpPublicUrl: "https://mcp.convy.app",
    authPublicUrl: "https://auth.convy.app",
    jwtIssuer: "https://auth.convy.app",
    jwtAudience: "https://mcp.convy.app",
    jwtPublicKeyPem: await exportSPKI(publicKey),
  });
  const server = app.listen(0);

  try {
    const port = (server.address() as AddressInfo).port;
    const response = await fetch(`http://127.0.0.1:${port}/mcp`, {
      method: "POST",
      headers: {
        authorization: `Bearer ${token}`,
        "content-type": "application/json",
      },
      body: JSON.stringify({ jsonrpc: "2.0", id: 1, method: "tools/list" }),
    });

    assert.notEqual(response.status, 403);
  } finally {
    server.close();
  }
});

test("MCP root endpoint is not a transport alias", async () => {
  const { publicKey, privateKey } = generateKeyPairSync("rsa", { modulusLength: 2048 });
  const token = await new SignJWT({
    sub: "11111111-1111-4111-8111-111111111111",
    token_use: "mcp_access",
    scope: supportedScopes.join(" "),
  })
    .setProtectedHeader({ alg: "RS256" })
    .setIssuer("https://auth.convy.app")
    .setAudience("https://mcp.convy.app")
    .setExpirationTime("5m")
    .sign(privateKey);
  const app = createApp({
    port: 0,
    apiBaseUrl: "https://api.convy.app",
    mcpPublicUrl: "https://mcp.convy.app",
    authPublicUrl: "https://auth.convy.app",
    jwtIssuer: "https://auth.convy.app",
    jwtAudience: "https://mcp.convy.app",
    jwtPublicKeyPem: await exportSPKI(publicKey),
  });
  const server = app.listen(0);

  try {
    const port = (server.address() as AddressInfo).port;
    const response = await fetch(`http://127.0.0.1:${port}/`, {
      method: "POST",
      headers: {
        authorization: `Bearer ${token}`,
        "content-type": "application/json",
        accept: "application/json, text/event-stream",
      },
      body: JSON.stringify({ jsonrpc: "2.0", id: 1, method: "tools/list", params: {} }),
    });

    assert.equal(response.status, 404);
  } finally {
    server.close();
  }
});
