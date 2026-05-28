export const readOnlyScopes = [
  "convy.households.read",
  "convy.lists.read",
  "convy.items.read",
  "convy.tasks.read",
  "convy.activity.read",
] as const;

export type ReadOnlyScope = (typeof readOnlyScopes)[number];

export const writeScopes = [
  "convy.items.write",
  "convy.tasks.write",
] as const;

export type WriteScope = (typeof writeScopes)[number];

export const supportedScopes = [
  ...readOnlyScopes,
  ...writeScopes,
] as const;

export type SupportedScope = (typeof supportedScopes)[number];

type MetadataOptions = {
  mcpPublicUrl: string;
  authPublicUrl: string;
};

export function createProtectedResourceMetadata(options: MetadataOptions) {
  const mcpPublicUrl = trimTrailingSlash(options.mcpPublicUrl);
  const authPublicUrl = trimTrailingSlash(options.authPublicUrl);

  return {
    resource: mcpPublicUrl,
    authorization_servers: [authPublicUrl],
    bearer_methods_supported: ["header"],
    scopes_supported: [...supportedScopes],
    resource_documentation: `${mcpPublicUrl}/docs`,
  };
}

export function createBearerChallenge(options: MetadataOptions, error?: string) {
  const metadataUrl = `${trimTrailingSlash(options.mcpPublicUrl)}/.well-known/oauth-protected-resource`;
  const scope = supportedScopes.join(" ");
  const parts = [`resource_metadata="${metadataUrl}"`, `scope="${scope}"`];

  if (error) {
    parts.push(`error="${error}"`);
  }

  return `Bearer ${parts.join(", ")}`;
}

function trimTrailingSlash(value: string) {
  return value.replace(/\/+$/, "");
}
