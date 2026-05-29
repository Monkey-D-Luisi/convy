import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL("..", import.meta.url));

test("consent screen states read permissions, limited write permissions, and exclusions", () => {
  const source = readFileSync(join(root, "components", "authorize-client.tsx"), "utf8");

  assert.match(source, /read access/);
  assert.match(source, /limited write access/);
  assert.match(source, /ChatGPT can create and update shopping items and tasks when you approve the action/);
  assert.match(source, /ChatGPT cannot edit, delete, archive, invite, leave, view admin metrics, access backups, modify account settings, or manage lists/);
  assert.match(source, /convy\.items\.write/);
  assert.match(source, /convy\.tasks\.write/);
});

test("auth app proxies approval to the backend OAuth broker", () => {
  const source = readFileSync(join(root, "app", "api", "oauth", "approve", "route.ts"), "utf8");

  assert.match(source, /CONVY_API_BASE_URL/);
  assert.match(source, /\/api\/v1\/mcp\/oauth\/authorize\/approve/);
  assert.match(source, /authorization/);
});

test("auth app supports Google sign-in for OAuth consent", () => {
  const source = readFileSync(join(root, "components", "authorize-client.tsx"), "utf8");

  assert.match(source, /GoogleAuthProvider/);
  assert.match(source, /signInWithPopup/);
  assert.match(source, /Sign in with Google/);
});

test("auth app does not contain Basic Auth handling", () => {
  const source = readFileSync(join(root, "components", "authorize-client.tsx"), "utf8");

  assert.doesNotMatch(source, /Basic/i);
});
