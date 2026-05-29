export const CONVY_WIDGET_RESOURCE_URI = "ui://widget/convy-summary-v1.html";
export const CONVY_WIDGET_DOMAIN = "https://mcp.convyapp.com";
export const CONVY_WIDGET_DESCRIPTION = "Interactive Convy household, shopping, task, and activity summary.";

export const CONVY_WIDGET_CSP = {
  connectDomains: [],
  resourceDomains: [],
  frameDomains: [],
} as const;

export const CONVY_WIDGET_LEGACY_CSP = {
  connect_domains: [],
  resource_domains: [],
  frame_domains: [],
  redirect_domains: [],
} as const;
