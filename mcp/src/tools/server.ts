import { performance } from "node:perf_hooks";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { CallToolResult } from "@modelcontextprotocol/sdk/types.js";
import { ConvyApiClient } from "../convy-api-client.js";
import type { McpAuthContext } from "../auth.js";
import type { McpConfig } from "../config.js";
import { ensureToolScopes, statusForError, toolDefinitions, type ToolDefinition } from "./definitions.js";

export function createConvyMcpServer(
  config: Pick<McpConfig, "apiBaseUrl" | "auditApiKey">,
  auth: McpAuthContext,
  apiClient = new ConvyApiClient({ baseUrl: config.apiBaseUrl, auditApiKey: config.auditApiKey }),
) {
  const server = new McpServer({
    name: "convy-mcp",
    version: "0.1.0",
  });

  for (const definition of toolDefinitions) {
    registerTool(server, definition, auth, apiClient);
  }

  return server;
}

function registerTool(
  server: McpServer,
  definition: ToolDefinition,
  auth: McpAuthContext,
  apiClient: ConvyApiClient,
) {
  server.registerTool(definition.name, {
    title: definition.title,
    description: definition.description,
    inputSchema: definition.inputSchema,
    outputSchema: definition.outputSchema,
    annotations: definition.annotations,
  }, async (args): Promise<CallToolResult> => {
    const startedAt = performance.now();
    let householdId: string | null | undefined;

    try {
      ensureToolScopes(definition, auth);
      const parsedArgs = definition.inputSchema.parse(args);
      const structuredContent = await definition.execute(parsedArgs, { apiClient, auth });
      householdId = structuredContent.meta.householdId;
      await recordAudit(apiClient, {
        userId: auth.userId,
        toolName: definition.name,
        householdId,
        status: "Success",
        latencyMs: elapsed(startedAt),
      });

      return {
        structuredContent,
        content: [{
          type: "text",
          text: summarize(definition.name, structuredContent),
        }],
      };
    } catch (error) {
      const status = statusForError(error);
      await recordAudit(apiClient, {
        userId: auth.userId,
        toolName: definition.name,
        householdId,
        status,
        latencyMs: elapsed(startedAt),
        errorType: status,
      });

      return {
        isError: true,
        content: [{
          type: "text",
          text: error instanceof Error ? error.message : "Convy MCP tool failed.",
        }],
      };
    }
  });
}

async function recordAudit(
  apiClient: ConvyApiClient,
  event: Parameters<ConvyApiClient["recordToolInvocation"]>[0],
) {
  try {
    await apiClient.recordToolInvocation(event);
  } catch {
    // Tool responses should not fail solely because audit delivery is temporarily unavailable.
  }
}

function elapsed(startedAt: number) {
  return Math.max(0, Math.round(performance.now() - startedAt));
}

function summarize(toolName: string, structuredContent: unknown) {
  if (typeof structuredContent !== "object" || structuredContent === null) {
    return `${toolName} returned Convy data.`;
  }

  const data = (structuredContent as { data?: unknown }).data;
  if (typeof data === "object" && data !== null && "selectionRequired" in data) {
    return "A household must be selected before this Convy data can be returned.";
  }

  return `${toolName} returned Convy data.`;
}
