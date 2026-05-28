import type { Request, Response } from "express";
import { importSPKI, jwtVerify } from "jose";
import { createBearerChallenge, supportedScopes } from "./metadata.js";
import type { McpConfig } from "./config.js";

export type McpAuthContext = {
  token: string;
  userId: string;
  clientId?: string;
  scopes: Set<string>;
};

let cachedPublicKeyPem: string | undefined;
let cachedPublicKey: CryptoKey | undefined;

export async function authenticateRequest(
  req: Request,
  res: Response,
  config: Pick<McpConfig, "jwtPublicKeyPem" | "jwtIssuer" | "jwtAudience" | "mcpPublicUrl" | "authPublicUrl">,
): Promise<McpAuthContext | null> {
  const token = extractBearerToken(req);
  if (!token) {
    sendUnauthorized(res, config);
    return null;
  }

  let auth: McpAuthContext;
  try {
    auth = await validateMcpToken(token, config);
  } catch {
    sendUnauthorized(res, config, "invalid_token");
    return null;
  }

  const hasSupportedScope = supportedScopes.some((scope) => auth.scopes.has(scope));
  if (!hasSupportedScope) {
    res.status(403).json({
      error: "insufficient_scope",
      supported_scopes: supportedScopes,
    });
    return null;
  }

  return auth;
}

export async function validateMcpToken(
  token: string,
  config: Pick<McpConfig, "jwtPublicKeyPem" | "jwtIssuer" | "jwtAudience">,
): Promise<McpAuthContext> {
  const publicKey = await getPublicKey(config.jwtPublicKeyPem);
  const { payload } = await jwtVerify(token, publicKey, {
    issuer: config.jwtIssuer,
    audience: config.jwtAudience,
  });

  if (payload.token_use !== "mcp_access") {
    throw new Error("Token is not an MCP access token.");
  }

  if (typeof payload.sub !== "string" || !isUuid(payload.sub)) {
    throw new Error("MCP token subject must be a Convy user ID.");
  }

  const scopeClaim = payload.scope;
  const scopes = new Set(typeof scopeClaim === "string" ? scopeClaim.split(/\s+/).filter(Boolean) : []);

  return {
    token,
    userId: payload.sub,
    clientId: typeof payload.client_id === "string" ? payload.client_id : undefined,
    scopes,
  };
}

function extractBearerToken(req: Request) {
  const authorization = req.header("authorization");
  if (!authorization?.toLowerCase().startsWith("bearer ")) {
    return null;
  }

  return authorization.slice("bearer ".length).trim();
}

function sendUnauthorized(
  res: Response,
  options: Pick<McpConfig, "mcpPublicUrl" | "authPublicUrl">,
  error?: string,
) {
  res.setHeader("WWW-Authenticate", createBearerChallenge(options, error));
  res.status(401).json({ error: error ?? "authorization_required" });
}

async function getPublicKey(publicKeyPem: string) {
  if (cachedPublicKeyPem === publicKeyPem && cachedPublicKey) {
    return cachedPublicKey;
  }

  cachedPublicKey = await importSPKI(publicKeyPem, "RS256");
  cachedPublicKeyPem = publicKeyPem;
  return cachedPublicKey;
}

function isUuid(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}
