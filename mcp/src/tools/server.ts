import { performance } from "node:perf_hooks";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import {
  registerAppResource,
  registerAppTool,
  RESOURCE_MIME_TYPE,
} from "@modelcontextprotocol/ext-apps/server";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { CallToolResult } from "@modelcontextprotocol/sdk/types.js";
import { ConvyApiClient } from "../convy-api-client.js";
import type { McpAuthContext } from "../auth.js";
import type { McpConfig } from "../config.js";
import { ensureToolScopes, statusForError, toolDefinitions, type ToolDefinition } from "./definitions.js";
import {
  CONVY_WIDGET_CSP,
  CONVY_WIDGET_DESCRIPTION,
  CONVY_WIDGET_DOMAIN,
  CONVY_WIDGET_LEGACY_CSP,
  CONVY_WIDGET_RESOURCE_URI,
} from "../widget-metadata.js";

type ToolDescriptorMetadata = {
  _meta: {
    ui: {
      resourceUri: typeof CONVY_WIDGET_RESOURCE_URI;
      visibility: ["model", "app"] | ["model"];
    };
    "openai/outputTemplate": typeof CONVY_WIDGET_RESOURCE_URI;
    "openai/toolInvocation/invoking": string;
    "openai/toolInvocation/invoked": string;
  };
};

const toolStatusText: Record<string, { invoking: string; invoked: string }> = {
  convy_get_context: {
    invoking: "Loading Convy households",
    invoked: "Convy households loaded",
  },
  convy_get_shopping_context: {
    invoking: "Loading Convy shopping lists",
    invoked: "Convy shopping lists loaded",
  },
  convy_get_shopping_list: {
    invoking: "Loading Convy shopping items",
    invoked: "Convy shopping items loaded",
  },
  convy_get_task_list: {
    invoking: "Loading Convy tasks",
    invoked: "Convy tasks loaded",
  },
  convy_get_recent_activity: {
    invoking: "Loading Convy activity",
    invoked: "Convy activity loaded",
  },
  convy_add_shopping_items: {
    invoking: "Adding Convy shopping items",
    invoked: "Convy shopping items updated",
  },
  convy_update_shopping_items_status: {
    invoking: "Updating Convy shopping items",
    invoked: "Convy shopping items updated",
  },
  convy_add_tasks: {
    invoking: "Adding Convy tasks",
    invoked: "Convy tasks updated",
  },
  convy_update_tasks_status: {
    invoking: "Updating Convy tasks",
    invoked: "Convy tasks updated",
  },
};

export function createConvyMcpServer(
  config: Pick<McpConfig, "apiBaseUrl" | "auditApiKey">,
  auth: McpAuthContext,
  apiClient = new ConvyApiClient({ baseUrl: config.apiBaseUrl, auditApiKey: config.auditApiKey }),
) {
  const server = new McpServer({
    name: "convy-mcp",
    version: "0.1.0",
  });

  registerConvyWidgetResource(server);

  for (const definition of toolDefinitions) {
    registerTool(server, definition, auth, apiClient);
  }

  return server;
}

export function createToolDescriptorMetadata(definition: ToolDefinition): ToolDescriptorMetadata {
  const status = toolStatusText[definition.name] ?? {
    invoking: "Checking Convy",
    invoked: "Convy updated",
  };

  return {
    _meta: {
      ui: {
        resourceUri: CONVY_WIDGET_RESOURCE_URI,
        visibility: definition.annotations.readOnlyHint ? ["model", "app"] : ["model"],
      },
      "openai/outputTemplate": CONVY_WIDGET_RESOURCE_URI,
      "openai/toolInvocation/invoking": status.invoking,
      "openai/toolInvocation/invoked": status.invoked,
    },
  };
}

function registerConvyWidgetResource(server: McpServer) {
  registerAppResource(
    server,
    "Convy Summary Widget",
    CONVY_WIDGET_RESOURCE_URI,
    {
      title: "Convy Summary",
      description: CONVY_WIDGET_DESCRIPTION,
      _meta: {
        ui: {
          prefersBorder: true,
          domain: CONVY_WIDGET_DOMAIN,
          csp: CONVY_WIDGET_CSP,
        },
        "openai/widgetDescription": CONVY_WIDGET_DESCRIPTION,
        "openai/widgetPrefersBorder": true,
        "openai/widgetCSP": CONVY_WIDGET_LEGACY_CSP,
        "openai/widgetDomain": CONVY_WIDGET_DOMAIN,
      },
    },
    async () => ({
      contents: [
        {
          uri: CONVY_WIDGET_RESOURCE_URI,
          mimeType: RESOURCE_MIME_TYPE,
          text: readWidgetHtml(),
          _meta: {
            ui: {
              prefersBorder: true,
              domain: CONVY_WIDGET_DOMAIN,
              csp: CONVY_WIDGET_CSP,
            },
            "openai/widgetDescription": CONVY_WIDGET_DESCRIPTION,
            "openai/widgetPrefersBorder": true,
            "openai/widgetCSP": CONVY_WIDGET_LEGACY_CSP,
            "openai/widgetDomain": CONVY_WIDGET_DOMAIN,
          },
        },
      ],
    }),
  );
}

function registerTool(
  server: McpServer,
  definition: ToolDefinition,
  auth: McpAuthContext,
  apiClient: ConvyApiClient,
) {
  registerAppTool(server, definition.name, {
    title: definition.title,
    description: definition.description,
    inputSchema: definition.inputSchema,
    outputSchema: definition.outputSchema,
    annotations: definition.annotations,
    ...createToolDescriptorMetadata(definition),
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
        clientId: auth.clientId,
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
        clientId: auth.clientId,
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

function readWidgetHtml() {
  const currentDir = dirname(fileURLToPath(import.meta.url));
  const candidates = [
    resolve(currentDir, "../../widget/convy-summary-v1.html"),
    resolve(currentDir, "../../../dist/widget/convy-summary-v1.html"),
    resolve(process.cwd(), "dist/widget/convy-summary-v1.html"),
  ];
  const existing = candidates.find((candidate) => existsSync(candidate));
  if (existing) {
    return readFileSync(existing, "utf8");
  }

  return [
    '<div id="root">',
    "Convy widget bundle is not available. Run npm run build:widget before deployment.",
    "</div>",
  ].join("");
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
