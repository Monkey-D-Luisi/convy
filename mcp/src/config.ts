import { readFileSync } from "node:fs";

export type McpConfig = {
  port: number;
  apiBaseUrl: string;
  mcpPublicUrl: string;
  authPublicUrl: string;
  jwtIssuer: string;
  jwtAudience: string;
  jwtAudienceAliases?: string[];
  mcpPublicUrlAliases?: string[];
  trustedProxy?: string;
  jwtPublicKeyPem: string;
  auditApiKey?: string;
  openAiAppsChallengeToken?: string;
};

export function loadConfig(env: NodeJS.ProcessEnv = process.env): McpConfig {
  const mcpPublicUrl = env.MCP_PUBLIC_URL ?? "https://mcp.convyapp.com";
  const authPublicUrl = env.AUTH_PUBLIC_URL ?? "https://auth.convyapp.com";
  const aliases = parseOrigins(env.MCP_PUBLIC_URL_ALIASES);
  const audienceAliases = parseOrigins(env.MCP_JWT_AUDIENCE_ALIASES);
  const audience = env.MCP_JWT_AUDIENCE ?? mcpPublicUrl;
  if (aliases.some((origin) => ![audience, ...audienceAliases].includes(origin))) {
    throw new Error("Every MCP public alias must have an explicitly accepted token audience.");
  }
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
    jwtAudience: audience,
    jwtAudienceAliases: audienceAliases,
    mcpPublicUrlAliases: aliases,
    trustedProxy: env.MCP_TRUSTED_PROXY || undefined,
    jwtPublicKeyPem: readPublicKey(env),
    auditApiKey,
    openAiAppsChallengeToken: env.OPENAI_APPS_CHALLENGE_TOKEN || undefined,
  };
}

function parseOrigins(value: string | undefined): string[] {
  if (!value) return [];
  return value.split(",").map((item) => {
    const url = new URL(item.trim());
    if (url.protocol !== "https:" || url.username || url.password || url.pathname !== "/" || url.search || url.hash) {
      throw new Error("MCP aliases must be HTTPS origins.");
    }
    return url.origin;
  });
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
