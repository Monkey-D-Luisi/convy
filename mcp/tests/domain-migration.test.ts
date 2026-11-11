import assert from "node:assert/strict";
import { generateKeyPairSync } from "node:crypto";
import { request } from "node:http";
import type { AddressInfo } from "node:net";
import { test } from "node:test";
import { exportSPKI, SignJWT } from "jose";
import { validateMcpToken } from "../src/auth.js";
import { loadConfig } from "../src/config.js";
import { createApp } from "../src/server.js";

const legacy = "https://mcp.convyapp.com";
const alias = "https://mcp-convy.monkeydluisi.com";
const issuer = "https://auth.convyapp.com";

test("parallel resources preserve issuer and advertise only allowlisted request hosts", async () => {
  const { publicKey, privateKey } = generateKeyPairSync("rsa", { modulusLength: 2048 });
  const config = loadConfig({
    CONVY_API_BASE_URL: "http://api:8080",
    MCP_JWT_PUBLIC_KEY: await exportSPKI(publicKey),
    MCP_PUBLIC_URL_ALIASES: alias,
    MCP_JWT_AUDIENCE_ALIASES: alias,
  });
  const server = createApp(config).listen(0);
  const port = (server.address() as AddressInfo).port;
  const call = (path: string, method: string, host: string) => new Promise<{ status: number; body: string; challenge?: string }>((resolve, reject) => {
    const req = request({ hostname: "127.0.0.1", port, path, method, headers: { host } }, (res) => {
      let body = "";
      res.on("data", (chunk) => { body += chunk; });
      res.on("end", () => resolve({ status: res.statusCode!, body, challenge: res.headers["www-authenticate"] }));
    });
    req.on("error", reject); req.end();
  });
  try {
    for (const origin of [legacy, alias, "https://attacker.example"]) {
      const expected = origin === alias ? alias : legacy;
      const host = new URL(origin).host;
      const metadata = await call("/.well-known/oauth-protected-resource", "GET", host);
      assert.equal(metadata.status, 200);
      const body = JSON.parse(metadata.body) as { resource: string; authorization_servers: string[] };
      assert.equal(body.resource, expected);
      assert.deepEqual(body.authorization_servers, [issuer]);
      const rejected = await call("/mcp", "POST", host);
      assert.equal(rejected.status, 401);
      assert.ok(rejected.challenge?.includes(`resource_metadata="${expected}/.well-known/oauth-protected-resource"`));
    }
    for (const audience of [legacy, alias, "https://attacker.example"]) {
      const token = await new SignJWT({ sub: "11111111-1111-4111-8111-111111111111", token_use: "mcp_access", scope: "convy.households.read" })
        .setProtectedHeader({ alg: "RS256" }).setIssuer(issuer).setAudience(audience).setExpirationTime("1m").sign(privateKey);
      if (audience === "https://attacker.example") await assert.rejects(validateMcpToken(token, config));
      else assert.equal((await validateMcpToken(token, config)).userId, "11111111-1111-4111-8111-111111111111");
    }
  } finally { server.close(); }
});

test("aliases reject non-origin URLs and insecure or credential-bearing origins", () => {
  for (const aliasValue of ["http://example.com", "https://user:password@example.com", "https://example.com/path", "https://example.com/?x=1", "https://example.com/#fragment"]) {
    assert.throws(() => loadConfig({ CONVY_API_BASE_URL: "http://api:8080", MCP_JWT_PUBLIC_KEY: "test", MCP_PUBLIC_URL_ALIASES: aliasValue }));
  }
  assert.throws(() => loadConfig({ CONVY_API_BASE_URL: "http://api:8080", MCP_JWT_PUBLIC_KEY: "test", MCP_PUBLIC_URL_ALIASES: alias }), /accepted token audience/);
});
