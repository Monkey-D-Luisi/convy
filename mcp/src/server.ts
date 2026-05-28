import express from "express";
import { StreamableHTTPServerTransport } from "@modelcontextprotocol/sdk/server/streamableHttp.js";
import { authenticateRequest } from "./auth.js";
import { loadConfig, type McpConfig } from "./config.js";
import { createProtectedResourceMetadata } from "./metadata.js";
import { createConvyMcpServer } from "./tools/server.js";

export function createApp(config: McpConfig) {
  const app = express();

  app.disable("x-powered-by");
  app.use(express.json({ limit: "1mb" }));

  app.get("/health", (_req, res) => {
    res.json({ status: "ok" });
  });

  app.get("/.well-known/oauth-protected-resource", (_req, res) => {
    res.json(createProtectedResourceMetadata(config));
  });

  const sendDocs = (_req: express.Request, res: express.Response) => {
    res.type("text/plain").send([
      "Convy MCP beta",
      "",
      "Read tools: convy_get_context, convy_get_household_overview, convy_get_lists, convy_get_shopping_items, convy_get_tasks, convy_get_recent_activity.",
      "Limited write tools: convy_create_shopping_item, convy_complete_shopping_item, convy_uncomplete_shopping_item, convy_create_task, convy_complete_task, convy_uncomplete_task.",
      "Scopes: convy.households.read, convy.lists.read, convy.items.read, convy.tasks.read, convy.activity.read, convy.items.write, convy.tasks.write.",
    ].join("\n"));
  };

  app.get("/docs", sendDocs);

  const handleMcpRequest = async (req: express.Request, res: express.Response) => {
    const auth = await authenticateRequest(req, res, config);
    if (!auth) {
      return;
    }

    const server = createConvyMcpServer(config, auth);
    const transport = new StreamableHTTPServerTransport({
      sessionIdGenerator: undefined,
    });

    try {
      await server.connect(transport);
      await transport.handleRequest(req, res, req.body);
    } finally {
      await server.close();
    }
  };

  app.post("/mcp", handleMcpRequest);

  return app;
}

if (import.meta.url === `file://${process.argv[1]}`) {
  const config = loadConfig();
  createApp(config).listen(config.port, () => {
    console.log(`Convy MCP server listening on port ${config.port}`);
  });
}
