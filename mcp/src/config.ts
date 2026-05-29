import { readFileSync } from "node:fs";

export type McpConfig = {
  port: number;
  apiBaseUrl: string;
  mcpPublicUrl: string;
  authPublicUrl: string;
  jwtIssuer: string;
  jwtAudience: string;
  jwtPublicKeyPem: string;
  auditApiKey?: string;
};

export function loadConfig(env: NodeJS.ProcessEnv = process.env): McpConfig {
  const mcpPublicUrl = env.MCP_PUBLIC_URL ?? "https://mcp.convyapp.com";
  const authPublicUrl = env.AUTH_PUBLIC_URL ?? "https://auth.convyapp.com";
  const auditApiKey = env.CONVY_MCP_AUDIT_API_KEY;
  if (env.NODE_ENV === "production" && !auditApiKey) {
    throw new Error("CONVY_MCP_AUDIT_API_KEY is required in production.");
  }

  return {
    port: Number.parseInt(env.PORT ?? "3001", 10),
    apiBaseUrl: trimTrailingSlash(requireEnv(env, "CONVY_API_BASE_URL")),
    mcpPublicUrl: trimTrailingSlash(mcpPublicUrl),
    authPublicUrl: trimTrailingSlash(authPublicUrl),
    jwtIssuer: env.MCP_JWT_ISSUER ?? authPublicUrl,
    jwtAudience: env.MCP_JWT_AUDIENCE ?? mcpPublicUrl,
    jwtPublicKeyPem: readPublicKey(env),
    auditApiKey,
  };
}

function readPublicKey(env: NodeJS.ProcessEnv) {
  if (env.MCP_JWT_PUBLIC_KEY) {
    return env.MCP_JWT_PUBLIC_KEY.replace(/\\n/g, "\n");
  }

  if (env.MCP_JWT_PUBLIC_KEY_BASE64) {
    return Buffer.from(env.MCP_JWT_PUBLIC_KEY_BASE64, "base64").toString("utf8");
  }

  if (env.MCP_JWT_PUBLIC_KEY_PATH) {
    return readFileSync(env.MCP_JWT_PUBLIC_KEY_PATH, "utf8");
  }

  throw new Error("MCP JWT public key is not configured.");
}

function requireEnv(env: NodeJS.ProcessEnv, name: string) {
  const value = env[name];
  if (!value) {
    throw new Error(`${name} is required.`);
  }

  return value;
}

function trimTrailingSlash(value: string) {
  return value.replace(/\/+$/, "");
}
