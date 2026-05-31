import assert from "node:assert/strict";
import { test } from "node:test";

import { loadConfig } from "../src/config.js";

test("MCP config defaults to convyapp.com public domains", () => {
  const config = loadConfig({
    CONVY_API_BASE_URL: "https://api.convyapp.com",
    MCP_JWT_PUBLIC_KEY: "-----BEGIN PUBLIC KEY-----\ntest\n-----END PUBLIC KEY-----",
  } as NodeJS.ProcessEnv);

  assert.equal(config.mcpPublicUrl, "https://mcp.convyapp.com");
  assert.equal(config.authPublicUrl, "https://auth.convyapp.com");
  assert.equal(config.jwtIssuer, "https://auth.convyapp.com");
  assert.equal(config.jwtAudience, "https://mcp.convyapp.com");
});

test("MCP config loads OpenAI Apps challenge token when configured", () => {
  const config = loadConfig({
    CONVY_API_BASE_URL: "https://api.convyapp.com",
    MCP_JWT_PUBLIC_KEY: "-----BEGIN PUBLIC KEY-----\ntest\n-----END PUBLIC KEY-----",
    OPENAI_APPS_CHALLENGE_TOKEN: "challenge-token",
  } as NodeJS.ProcessEnv);

  assert.equal(config.openAiAppsChallengeToken, "challenge-token");
});
